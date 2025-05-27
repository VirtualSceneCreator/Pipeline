from langchain_chroma import Chroma
from langchain_core.example_selectors import SemanticSimilarityExampleSelector
from langchain_core.prompts import FewShotPromptTemplate, PromptTemplate
from langchain_openai import OpenAIEmbeddings

from Examples import examples


class ExampleSelector:
    @staticmethod
    def get_similar_examples(prompt: str, count: int = 1) -> list[str]:
        example_prompt = PromptTemplate(
            input_variables=["input", "output"],
            template="Example action plan: {{input}}\nExample Output: {{output}}",
            template_format="jinja2",
        )

        example_selector = SemanticSimilarityExampleSelector.from_examples(
            examples,
            OpenAIEmbeddings(),
            Chroma,
            k=count,
        )
        similar_prompt = FewShotPromptTemplate(
            example_selector=example_selector,
            example_prompt=example_prompt,
            prefix="",
            suffix="",
            input_variables=["prompt"],
            template_format="jinja2"
        )

        return similar_prompt.format(prompt=prompt)
