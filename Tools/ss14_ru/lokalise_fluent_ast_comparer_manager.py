from fluent.syntax import ast
from fluentast import FluentAstMessage
from fluentastcomparer import FluentAstComparer
from fluentastmanager import FluentAstManager


class LokaliseFluentAstComparerManager:
    def __init__(self, source_parsed: ast.Resource, target_parsed: ast.Resource):
        self.source_parsed = source_parsed  # Исправлена опечатка "sourse" -> "source"
        self.target_parsed = target_parsed
        self.comparer = FluentAstComparer(source_parsed, target_parsed)
        self.ast_manager = FluentAstManager(source_parsed, target_parsed)

    def for_update(self) -> list:
        """
        Возвращает элементы, которые существуют в обоих источниках, но различаются по значению или атрибутам.

        Returns:
            Список элементов для обновления.
        """
        for_update = self.comparer.get_not_equal_exist_values_with_attrs()
        return for_update if for_update else []

    def update(self, for_update: list) -> ast.Resource:
        """
        Обновляет элементы в source_parsed на основе соответствующих элементов из target_parsed.

        Args:
            for_update: Список элементов для обновления.

        Returns:
            Обновленный source_parsed.
        """
        for update in for_update:
            try:
                idx = self.source_parsed.body.index(update.element)  # Используем исходный список напрямую
                update_mess = self.comparer.find_message_by_id_name(
                    update.get_id_name(), self.comparer.target_elements
                )
                if update_mess:
                    self.ast_manager.update_by_index(idx, update_mess.element)
                else:
                    print(f"Не найден соответствующий элемент в target для ключа {update.get_id_name()}")
            except ValueError:
                print(f"Элемент {update.get_id_name()} не найден в source_parsed")
            except Exception as e:
                print(f"Ошибка обновления элемента {update.get_id_name()}: {e}")
        return self.source_parsed

    def for_delete(self) -> list:
        """
        Возвращает элементы из target, отсутствующие в source (возможные кандидаты для удаления из Lokalise).

        Returns:
            Список элементов для удаления.
        """
        for_delete = self.comparer.get_not_exist_id_names()
        if for_delete:
            keys = [el.get_id_name() for el in for_delete]
            print(f"Следующие ключи есть в Lokalise, но отсутствуют в файле. Возможно, удалить из Lokalise: {keys}")
        return for_delete

    def for_create(self) -> list:
        """
        Возвращает элементы из source, отсутствующие в target (нужно добавить в Lokalise).

        Returns:
            Список элементов для создания.
        """
        for_create = self.comparer.get_not_equal_id_names()
        if for_create:
            keys = [el.get_id_name() for el in for_create]
            print(f"Следующие ключи из файла отсутствуют в Lokalise. Необходимо добавить: {keys}")
        return for_create


# Пример использования
if __name__ == "__main__":
    from fluent.syntax import FluentParser, FluentSerializer
    parser = FluentParser()
    serializer = FluentSerializer()

    source_data = """
    msg1 = Hello
    msg2 = World
    """
    target_data = """
    msg1 = Hi
    msg3 = Test
    """
    source_parsed = parser.parse(source_data)
    target_parsed = parser.parse(target_data)

    manager = LokaliseFluentAstComparerManager(source_parsed, target_parsed)

    # Поиск элементов для обновления
    to_update = manager.for_update()
    if to_update:
        updated_source = manager.update(to_update)
        print("Обновленный source:")
        print(serializer.serialize(updated_source))

    # Поиск элементов для удаления и создания
    manager.for_delete()
    manager.for_create()