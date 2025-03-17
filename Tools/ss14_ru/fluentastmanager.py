from fluent.syntax import ast
from fluentast import FluentAstAbstract


class FluentAstManager:
    def __init__(self, source_parsed: ast.Resource, target_parsed: ast.Resource):
        self.source_parsed = source_parsed  # Исправлена опечатка "sourse" -> "source"
        self.target_parsed = target_parsed
        # Преобразуем элементы тела в объекты FluentAst, фильтруя None
        self.source_elements = [e for e in map(FluentAstAbstract.create_element, source_parsed.body) if e]
        self.target_elements = [e for e in map(FluentAstAbstract.create_element, target_parsed.body) if e]

    def update_by_index(self, index: int, update_element: ast.Message) -> ast.Resource:
        """Обновляет элемент в source_parsed по указанному индексу."""
        try:
            # Проверяем, существует ли элемент по индексу
            if index < 0 or index >= len(self.source_parsed.body):
                raise IndexError(f"Индекс {index} вне диапазона (0, {len(self.source_parsed.body) - 1})")
            
            # Обновляем элемент
            self.source_parsed.body[index] = update_element
            return self.source_parsed
        
        except IndexError as e:
            raise IndexError(str(e)) from e
        except Exception as e:
            raise RuntimeError(f"Ошибка при обновлении элемента с индексом {index}: {e}") from e


# Пример использования (для тестирования)
if __name__ == "__main__":
    from fluent.syntax import FluentParser, FluentSerializer
    parser = FluentParser()
    serializer = FluentSerializer()

    # Создаем тестовые данные
    source_data = """
    msg1 = Hello
    msg2 = World
    """
    target_data = """
    msg1 = Hi
    """
    source_parsed = parser.parse(source_data)
    target_parsed = parser.parse(target_data)

    # Создаем менеджер
    manager = FluentAstManager(source_parsed, target_parsed)

    # Обновляем элемент по индексу 1
    new_message = ast.Message(
        id=ast.Identifier("msg2"),
        value=ast.Pattern([ast.TextElement("Updated World")])
    )
    try:
        updated_source = manager.update_by_index(1, new_message)
        print("Updated source:")
        print(serializer.serialize(updated_source))
    except Exception as e:
        print(f"Ошибка: {e}")