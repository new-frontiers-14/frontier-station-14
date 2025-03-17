from fluent.syntax import ast
from fluentast import FluentAstAbstract
from pydash import py_


class FluentAstComparer:
    def __init__(self, source_parsed: ast.Resource, target_parsed: ast.Resource):
        self.source_parsed = source_parsed  # Исправлена опечатка "sourse" -> "source"
        self.target_parsed = target_parsed
        # Преобразуем элементы тела в объекты FluentAst, фильтруя None
        self.source_elements = [el for el in map(FluentAstAbstract.create_element, source_parsed.body) if el]
        self.target_elements = [el for el in map(FluentAstAbstract.create_element, target_parsed.body) if el]

    def get_equal_elements(self) -> list:
        """Возвращает полностью эквивалентные сообщения (игнорируя span)."""
        return py_.intersection_with(
            self.source_elements,
            self.target_elements,
            comparator=lambda a, b: a.element.equals(b.element, ignored_fields=['span'])
        )

    def get_not_equal_elements(self) -> list:
        """Возвращает полностью неэквивалентные сообщения из source (игнорируя span)."""
        return py_.difference_with(
            self.source_elements,
            self.target_elements,
            comparator=lambda a, b: a.element.equals(b.element, ignored_fields=['span'])
        )

    def get_equal_id_names(self) -> list:
        """Возвращает сообщения с эквивалентными именами ключей."""
        return py_.intersection_with(
            self.source_elements,
            self.target_elements,
            comparator=lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'value', 'comment', 'attributes'])
        )

    def get_not_equal_id_names(self) -> list:
        """Возвращает сообщения из source с неэквивалентными именами ключей."""
        return py_.difference_with(
            self.source_elements,
            self.target_elements,
            comparator=lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'value', 'comment', 'attributes'])
        )

    def get_exist_id_names(self, source: list, target: list) -> list:
        """Возвращает сообщения из target, существующие в source по имени ключа."""
        return py_.intersection_with(
            source,
            target,
            comparator=lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'value', 'comment', 'attributes'])
        )

    def get_not_exist_id_names(self) -> list:
        """Возвращает сообщения из target, отсутствующие в source по имени ключа."""
        return py_.difference_with(
            self.target_elements,
            self.source_elements,
            comparator=lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'value', 'comment', 'attributes'])
        )

    def get_equal_values_with_attrs(self) -> list:
        """Возвращает сообщения с эквивалентными значениями и атрибутами."""
        return py_.intersection_with(
            self.target_elements,
            self.source_elements,
            comparator=lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'id', 'comment'])
        )

    def get_not_equal_values_with_attrs(self) -> list:
        """Возвращает сообщения из source с неэквивалентными значениями и атрибутами."""
        return py_.difference_with(
            self.source_elements,
            self.target_elements,
            comparator=lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'id', 'comment'])
        )

    def get_not_equal_exist_values_with_attrs(self) -> list:
        """Возвращает сообщения из source, существующие в target, но с неэквивалентными значениями и атрибутами."""
        diff = self.get_not_equal_values_with_attrs()
        exist = self.get_exist_id_names(self.source_elements, self.target_elements)
        return py_.intersection(diff, exist)

    def get_target_not_equal_values_with_attrs(self) -> list:
        """Возвращает сообщения из target с неэквивалентными значениями и атрибутами."""
        return py_.difference_with(
            self.target_elements,
            self.source_elements,
            comparator=lambda a, b: a.element.equals(b.element, ignored_fields=['span', 'id', 'comment'])
        )

    def get_target_not_equal_exist_values_with_attrs(self) -> list:
        """Возвращает сообщения из target, существующие в source, но с неэквивалентными значениями и атрибутами."""
        diff = self.get_target_not_equal_values_with_attrs()
        exist = self.get_exist_id_names(self.target_elements, self.source_elements)
        return py_.intersection(diff, exist)

    def find_message_by_id_name(self, id_name: str, elements: list) -> typing.Optional[typing.Any]:
        """Находит сообщение в списке по имени ключа."""
        return py_.find(elements, lambda el: el.get_id_name() == id_name)


# Пример использования (для тестирования)
if __name__ == "__main__":
    from fluent.syntax import FluentParser
    parser = FluentParser()

    source_data = """
    msg1 = Hello
    msg2 = World
    """
    target_data = """
    msg1 = Hello
    msg3 = Test
    """
    source_parsed = parser.parse(source_data)
    target_parsed = parser.parse(target_data)

    comparer = FluentAstComparer(source_parsed, target_parsed)
    print("Equal elements:", [e.get_id_name() for e in comparer.get_equal_elements()])
    print("Not equal elements:", [e.get_id_name() for e in comparer.get_not_equal_elements()])
    print("Equal ID names:", [e.get_id_name() for e in comparer.get_equal_id_names()])
    print("Not equal ID names:", [e.get_id_name() for e in comparer.get_not_equal_id_names()])