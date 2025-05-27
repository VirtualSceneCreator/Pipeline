# evaluation_and_report_seaborn.py

import os
import sys
import json
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
import matplotlib.image as mpimg     # ← add this import
from matplotlib.backends.backend_pdf import PdfPages
import textwrap

# Configuration
ROOT_DIR = r"referenceDirHere"
OUTPUT_CSV = "evaluation_results.csv"
OUTPUT_PDF = "evaluation_summary_extended.pdf"

SCHEMA_CHECKS = {
    "sceneName":      ["scene", "sceneName"],
    "environment":    ["scene", "environment"],
    "env.type":       ["scene", "environment", "type"],
    "env.dimensions": ["scene", "environment", "dimensions"],
    "env.lighting":   ["scene", "environment", "lighting"],
    "env.background": ["scene", "environment", "background"],
    "objectGroups":   ["scene", "objectGroups"],
    "objects":        ["scene", "objects"],
    "obj.objectId":   ["scene", "objects", 0, "objectId"],
    "obj.objectType": ["scene", "objects", 0, "objectType"],
    "obj.position":   ["scene", "objects", 0, "position"],
    "obj.rotation":   ["scene", "objects", 0, "rotation"],
    "obj.dimensions": ["scene", "objects", 0, "dimensions"],
}

def find_json_files(directory):
    files = []
    for dp, _, fnames in os.walk(directory):
        for f in fnames:
            if f.lower().endswith(".json"):
                files.append(os.path.join(dp, f))
    return files

def validate_structure(scene):
    required = {"sceneName", "environment", "objects"}
    present = set(scene.keys())
    missing = required - present
    extra = present - required - {"objectGroups"}
    return (len(missing) == 0), missing, extra

def check_path(data, path):
    cur = data
    for p in path:
        if isinstance(p, str) and isinstance(cur, dict) and p in cur:
            cur = cur[p]
        elif isinstance(p, int) and isinstance(cur, list) and 0 <= p < len(cur):
            cur = cur[p]
        else:
            return False
    return True

def compute_depth(obj, depth=0):
    if isinstance(obj, dict):
        return max([compute_depth(v, depth+1) for v in obj.values()] or [depth])
    if isinstance(obj, list):
        return max([compute_depth(i, depth+1) for i in obj] or [depth])
    return depth

def evaluate():
    paths = find_json_files(ROOT_DIR)
    if not paths:
        print("No JSON files found in directory:", ROOT_DIR)
        sys.exit(1)
    records = []
    for path in paths:
        rel = os.path.relpath(path, ROOT_DIR)
        try:
            data = json.load(open(path, encoding="utf-8"))
            scene = data.get("scene", {})
            valid, missing, extra = validate_structure(scene)
            num_objs = len(scene.get("objects", []))
            num_grps = len(scene.get("objectGroups", {}))
            depth = compute_depth(data)
            reason = ""
            if not valid:
                parts = []
                if missing:
                    parts.append("missing: " + ", ".join(sorted(missing)))
                if extra:
                    parts.append("extra: " + ", ".join(sorted(extra)))
                reason = "; ".join(parts)
            presence = {f"has_{k}": check_path(data, p) for k, p in SCHEMA_CHECKS.items()}
            rec = {
                "file": rel,
                "valid": valid,
                "missing_keys": ", ".join(sorted(missing)),
                "extra_keys": ", ".join(sorted(extra)),
                "num_objects": num_objs,
                "num_object_groups": num_grps,
                "json_depth": depth,
                "reason": reason
            }
            rec.update(presence)
        except Exception as e:
            rec = {
                "file": rel,
                "valid": False,
                "missing_keys": "",
                "extra_keys": "",
                "num_objects": None,
                "num_object_groups": None,
                "json_depth": None,
                "reason": f"Parse error: {e}"
            }
            for k in SCHEMA_CHECKS:
                rec[f"has_{k}"] = False
        records.append(rec)
    df = pd.DataFrame(records)
    df.to_csv(OUTPUT_CSV, index=False)
    return df

def plot_and_report(df):
    sns.set_theme(style="whitegrid")
    N = len(df)
    V = df["valid"].sum()
    I = N - V
    pctV = V / N * 100 if N else 0

    # Plot 1: Valid vs Invalid
    fig = plt.figure(figsize=(6,4))
    ax = sns.barplot(x=["Valid","Invalid"], y=[V,I])
    ax.set_title("Validity of MLDS JSON Files")
    ax.set_xlabel("File Classification")
    ax.set_ylabel("Number of Files")
    fig.tight_layout()
    fig.savefig("valid_invalid.png")
    plt.close(fig)

    # Plot 2: Object Count Distribution
    fig = plt.figure(figsize=(6,4))
    ax = sns.histplot(df["num_objects"].dropna(), bins=10, kde=False)
    ax.set_title("Distribution of Top-Level Object Counts")
    ax.set_xlabel("Number of Top-Level Objects")
    ax.set_ylabel("Number of Files")
    fig.tight_layout()
    fig.savefig("objects_distribution.png")
    plt.close(fig)

    # Plot 3: Object Groups Distribution
    fig = plt.figure(figsize=(6,4))
    ax = sns.histplot(df["num_object_groups"].dropna(), bins=10, kde=False)
    ax.set_title("Distribution of Object Group Counts")
    ax.set_xlabel("Number of Object Groups")
    ax.set_ylabel("Number of Files")
    fig.tight_layout()
    fig.savefig("groups_distribution.png")
    plt.close(fig)

    # Plot 4: JSON Depth Distribution
    fig = plt.figure(figsize=(6,4))
    ax = sns.histplot(df["json_depth"].dropna(), bins=10, kde=False)
    ax.set_title("Distribution of JSON Nesting Depths")
    ax.set_xlabel("Nesting Depth")
    ax.set_ylabel("Number of Files")
    fig.tight_layout()
    fig.savefig("depth_distribution.png")
    plt.close(fig)

    # Plot 5: Schema Component Presence
    presence_vals = [df[f"has_{k}"].sum()/N*100 for k in SCHEMA_CHECKS]
    fig = plt.figure(figsize=(8,6))
    ax = sns.barplot(x=presence_vals, y=list(SCHEMA_CHECKS.keys()), orient='h')
    ax.set_title("Schema Component Presence (%)")
    ax.set_xlabel("Presence (%)")
    ax.set_ylabel("Schema Component")
    fig.tight_layout()
    fig.savefig("presence_summary.png")
    plt.close(fig)

    missing_files = {k: df.loc[~df[f"has_{k}"], "file"].tolist() for k in SCHEMA_CHECKS}

    with PdfPages(OUTPUT_PDF) as pdf:
        # Page 1: Summary
        fig, ax = plt.subplots(figsize=(8.27,11.69))
        ax.axis("off")
        summary = [
            f"N = {N}",
            f"V = {V} ({pctV:.1f}%)",
            f"I = {I}",
            "",
            "Formulas:",
            "  V = sum(valid)",
            "  I = N - V",
            "  %V = V / N * 100",
            "",
            "Presence per Schema Component:"
        ]
        for k in SCHEMA_CHECKS:
            cnt = df[f"has_{k}"].sum()
            summary.append(f"  {k}: {cnt}/{N} ({cnt/N*100:.1f}%)")
        wrapped = "\n".join(textwrap.fill(line, 80) for line in summary)
        ax.text(0.01,0.99,wrapped,va="top",family="monospace",fontsize=10)
        pdf.savefig(fig)
        plt.close(fig)

        # Page 2: Invalid Files
        fig, ax = plt.subplots(figsize=(8.27,11.69))
        ax.axis("off")
        y = 0.99
        ax.text(0.01,y,"Invalid Files and Reasons:",va="top",fontsize=12,weight="bold")
        y -= 0.03
        for _, row in df[~df["valid"]].iterrows():
            text = f"{row['file']}: {row['reason']}"
            for line in textwrap.wrap(text, 90):
                ax.text(0.01,y,line,va="top",fontsize=9)
                y -= 0.02
                if y < 0.05:
                    pdf.savefig(fig); plt.close(fig)
                    fig, ax = plt.subplots(figsize=(8.27,11.69)); ax.axis("off"); y = 0.99
        pdf.savefig(fig); plt.close(fig)

        # Page 3: Missing Schema Parts
        fig, ax = plt.subplots(figsize=(8.27,11.69))
        ax.axis("off")
        y = 0.99
        ax.text(0.01,y,"Missing Schema Parts (Files):",va="top",fontsize=12,weight="bold")
        y -= 0.03
        for k in SCHEMA_CHECKS:
            header = f"{k}: present {df[f'has_{k}'].sum()}/{N}"
            for line in textwrap.wrap(header, 80):
                ax.text(0.01,y,line,va="top",fontsize=9,weight="bold")
                y -= 0.02
                if y < 0.05:
                    pdf.savefig(fig); plt.close(fig)
                    fig, ax = plt.subplots(figsize=(8.27,11.69)); ax.axis("off"); y = 0.99
            for f in missing_files[k]:
                for line in textwrap.wrap(f"- {f}", 90):
                    ax.text(0.03,y,line,va="top",fontsize=8)
                    y -= 0.015
                    if y < 0.05:
                        pdf.savefig(fig); plt.close(fig)
                        fig, ax = plt.subplots(figsize=(8.27,11.69)); ax.axis("off"); y = 0.99
            y -= 0.02
        pdf.savefig(fig); plt.close(fig)

        # Pages 4+: Include images
        for img in ["valid_invalid.png","objects_distribution.png","groups_distribution.png","depth_distribution.png","presence_summary.png"]:
            if os.path.exists(img):
                img_fig = plt.figure(figsize=(8.27,11.69))
                img_ax = img_fig.add_subplot(111)
                img_ax.imshow(mpimg.imread(img))
                img_ax.axis("off")
                pdf.savefig(img_fig); plt.close(img_fig)

def print_summary(df):
    N = len(df)
    V = df["valid"].sum()
    I = N - V
    pctV = V/N*100 if N else 0
    print("\n--- Evaluation Summary ---")
    print(f"N (Total files): {N}")
    print(f"V (Valid): {V} ({pctV:.1f}%)")
    print(f"I (Invalid): {I} ({100-pctV:.1f}%)")
    print("\nPresence per Schema Component:")
    for k in SCHEMA_CHECKS:
        cnt = df[f"has_{k}"].sum()
        pct = cnt/N*100 if N else 0
        print(f"  {k}: {cnt}/{N} ({pct:.1f}%)")
    print("\nInvalid Files and Reasons:")
    for _, row in df[~df["valid"]].iterrows():
        print(f"  {row['file']}: {row['reason']}")
    print("\nMissing Schema Parts (Files):")
    missing_files = {k: df.loc[~df[f"has_{k}"], "file"].tolist() for k in SCHEMA_CHECKS}
    for k, files in missing_files.items():
        print(f"  {k}: {len(files)} missing")
        for f in files:
            print(f"    - {f}")

if __name__ == "__main__":
    df = evaluate()
    print_summary(df)
    plot_and_report(df)
    print("\nReport created at", OUTPUT_PDF)
