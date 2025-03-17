import typing
from fluent.syntax import ast, FluentParser, FluentSerializer
from lokalisemodels import LokaliseKey
from pydash import py_


class FluentAstAbstract:
    @classmethod
    def get_id_name(cls, element: typing.Any) -> typing.Optional[str]:
        """Получает имя идентификатора элемента AST."""
        if isinstance(element, ast.Junk):
            return FluentAstJunk(element).get_id_name()
        elif isinstance(element, ast.Message):
            return FluentAstMessage(element).get_id_name()
        elif isinstance(element, ast.Term):
            return FluentAstTerm(element).get_id_name()
        return None

    @classmethod
    def create_element(cls, element: typing.Any) -> typing.Optional[typing.Any]:
        """Создает объект FluentAst на основе типа элемента."""
        if isinstance(element, ast.Junk):
            return FluentAstJunk(element)
        elif isinstance(element, ast.Message):
            return FluentAstMessage(element)
        elif isinstance(element, ast.Term):
            return FluentAstTerm(element)
        return None


class FluentAstMessage:
    def __init__(self, message: ast.Message):
        self.message = message
        self.element = message  # Сохраняем ссылку для совместимости

    def get_id_name(self) -> str:
        return self.message.id.name


class FluentAstTerm:
    def __init__(self, term: ast.Term):
        self.term = term
        self.element = term  # Сохраняем ссылку для совместимости

    def get_id_name(self) -> str:
        return self.term.id.name


class FluentAstAttribute:
    def __init__(self, id: str, value: str, parent_key: typing.Optional[str] = None):
        self.id = id
        self.value = value
        self.parent_key = parent_key


class FluentAstAttributeFactory:
    @classmethod
    def from_yaml_element(cls, element: typing.Any) -> typing.Optional[list[FluentAstAttribute]]:
        """Создает список атрибутов из YAML-элемента."""
        attrs = []
        if getattr(element, 'description', None):
            attrs.append(FluentAstAttribute('desc', element.description))
        if getattr(element, 'suffix', None):
            attrs.append(FluentAstAttribute('suffix', element.suffix))
        return attrs if attrs else None


class FluentAstJunk:
    def __init__(self, junk: ast.Junk):
        self.junk = junk
        self.element = junk  # Сохраняем ссылку для совместимости

    def get_id_name(self) -> str:
        # Предполагаем, что Junk содержит строку вида "key = value"
        return self.junk.content.split('=', 1)[0].strip()


class FluentSerializedMessage:
    @classmethod
    def from_yaml_element(cls, id: str, value: typing.Optional[str], attributes: typing.Optional[list[FluentAstAttribute]],
                         parent_id: typing.Optional[str] = None, raw_key: bool = False) -> typing.Optional[str]:
        """Создает сериализованное сообщение из YAML-элемента."""
        if not value and not id and not parent_id:
            return None

        attributes = attributes or []
        # Добавляем атрибут desc, если его нет
        if not any(attr.id == 'desc' for attr in attributes):
            desc_value = f"{{ {cls.get_key(parent_id)}.desc }}" if parent_id else '{ "" }'
            attributes.append(FluentAstAttribute('desc', desc_value))

        message = f"{cls.get_key(id, raw_key)} = {cls.get_value(value, parent_id)}\n"
        full_message = message

        for attr in attributes:
            fluent_newlines = attr.value.replace("\n", "\n        ")  # Форматирование отступов
            full_message = cls.add_attr(full_message, attr.id, fluent_newlines, raw_key=raw_key)

        return cls.to_serialized_message(full_message)

    @classmethod
    def from_lokalise_keys(cls, keys: typing.List[LokaliseKey]) -> str:
        """Создает сериализованные сообщения из списка ключей Lokalise."""
        # Группируем атрибуты по родительскому ключу
        attributes_keys = [k for k in keys if k.is_attr]
        attributes = [
            FluentAstAttribute(
                id=f".{k.get_key_last_name(k.key_name)}",
                value=cls.get_attr(k, k.get_key_last_name(k.key_name), k.get_parent_key()),
                parent_key=k.get_parent_key()
            ) for k in attributes_keys
        ]
        attributes_group = py_.group_by(attributes, 'parent_key')

        serialized_message = ''
        for key in keys:
            if key.is_attr:
                continue
            key_name = key.get_key_last_name(key.key_name)
            key_value = key.get_translation('ru').data['translation']
            key_attributes = attributes_group.get(f"{key.get_key_base_name(key.key_name)}.{key_name}", [])

            full_message = cls.from_yaml_element(key_name, key_value, key_attributes, key.get_parent_key(), raw_key=True)
            if full_message:
                serialized_message += f"\n{full_message}"
            elif message := key.serialize_message():
                serialized_message += f"\n{message}"
            else:
                raise ValueError(f"Ошибка сериализации ключа {key.key_name}")

        return serialized_message.lstrip()  # Убираем начальную пустую строку

    @staticmethod
    def get_attr(k: LokaliseKey, name: str, parent_id: typing.Optional[str] = None) -> str:
        """Получает значение атрибута."""
        if parent_id:
            return f"{{ {parent_id}.{name} }}"
        return k.get_translation('ru').data['translation']

    @staticmethod
    def to_serialized_message(string_message: str) -> str:
        """Сериализует строку в формат Fluent."""
        if not string_message:
            return ''
        ast_message = FluentParser().parse(string_message)
        return FluentSerializer(with_junk=True).serialize(ast_message) or ''

    @staticmethod
    def add_attr(message_str: str, attr_key: str, attr_value: str, raw_key: bool = False) -> str:
        """Добавляет атрибут к сообщению."""
        prefix = '' if raw_key else '.'
        return f"{message_str}  {prefix}{attr_key} = {attr_value}\n"

    @staticmethod
    def get_value(value: typing.Optional[str], parent_id: typing.Optional[str]) -> str:
        """Получает значение для сообщения."""
        if value:
            return value
        elif parent_id:
            return f"{{ {FluentSerializedMessage.get_key(parent_id)} }}"
        return '{ "" }'

    @staticmethod
    def get_key(id: str, raw: bool = False) -> str:
        """Формирует ключ сообщения."""
        return id if raw else f"ent-{id}"


# Пример использования (для тестирования)
if __name__ == "__main__":
    # Пример требует реализации LokaliseKey, поэтому просто заглушка
    class MockLokaliseKey:
        def __init__(self, key_name, translation, is_attr=False):
            self.key_name = key_name
            self.is_attr = is_attr
            self.translation_data = {'ru': {'translation': translation}}

        def get_key_last_name(self, key): return key.split('.')[-1]
        def get_key_base_name(self, key): return '.'.join(key.split('.')[:-1])
        def get_parent_key(self): return None
        def get_translation(self, lang): return self
        def serialize_message(self): return f"{self.key_name} = {self.translation_data['ru']['translation']}"

    keys = [
        MockLokaliseKey("Item.Name", "Название предмета"),
        MockLokaliseKey("Item.Name.desc", "Описание предмета", is_attr=True)
    ]
    result = FluentSerializedMessage.from_lokalise_keys(keys)
    print("Serialized output:")
    print(result)