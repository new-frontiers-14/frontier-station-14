import typing
import os
from pydash import py_
from project import Project


class LocalePath:
    def __init__(self, relative_file_path: str):
        """
        Инициализирует пути к файлам локалей для русского и английского языков.

        Args:
            relative_file_path: Относительный путь к файлу (без указания локали).
        """
        self.ru = os.path.join(Project().ru_locale_dir_path, relative_file_path)
        self.en = os.path.join(Project().en_locale_dir_path, relative_file_path)


class LokaliseTranslation:
    def __init__(self, data: dict, key_name: str):
        """
        Представляет перевод для ключа Lokalise.

        Args:
            data: Данные перевода из API Lokalise.
            key_name: Имя ключа перевода.
        """
        self.key_name = key_name  # Убрана лишняя запятая, приводящая к кортежу
        self.data = data


class LokaliseKey:
    def __init__(self, data: typing.Any):
        """
        Представляет ключ перевода Lokalise.

        Args:
            data: Данные ключа из API Lokalise (предположительно KeyModel).
        """
        self.data = data
        self.key_name = self.data.key_name['web']
        self.key_base_name = self.get_key_base_name(self.key_name)
        self.is_attr = self.check_is_attr()

    def get_file_path(self) -> LocalePath:
        """
        Возвращает объект LocalePath с путями к файлам для русской и английской локалей.

        Returns:
            Объект LocalePath с путями ru и en.
        """
        # Преобразуем ключ в относительный путь (например, "Space::Item" → "Space/Item.ftl")
        relative_parts = self.key_name.split('.')[0].split('::')
        relative_file_path = f"{'/'.join(relative_parts)}.ftl"
        return LocalePath(relative_file_path)

    def get_key_base_name(self, key_name: str) -> str:
        """
        Возвращает базовое имя ключа (первая часть до точки).

        Args:
            key_name: Полное имя ключа.

        Returns:
            Базовое имя ключа.
        """
        return key_name.split('.')[0]

    def get_key_last_name(self, key_name: str) -> str:
        """
        Возвращает последнюю часть имени ключа после точки.

        Args:
            key_name: Полное имя ключа.

        Returns:
            Последняя часть имени ключа.
        """
        return key_name.split('.')[-1]  # Заменено py_.last на встроенную логику

    def get_parent_key(self) -> typing.Optional[str]:
        """
        Возвращает родительский ключ, если текущий ключ является атрибутом.

        Returns:
            Родительский ключ или None, если ключ не атрибут.
        """
        if self.is_attr:
            return '.'.join(self.key_name.split('.')[:-1])
        return None

    def check_is_attr(self) -> bool:
        """
        Проверяет, является ли ключ атрибутом (содержит более одной точки).

        Returns:
            True, если ключ — атрибут, иначе False.
        """
        return len(self.key_name.split('.')) > 2

    def serialize(self) -> str:
        """
        Сериализует ключ в строку формата Fluent.

        Returns:
            Строка в формате сообщения или атрибута.
        """
        return self.serialize_attr() if self.is_attr else self.serialize_message()

    def serialize_attr(self) -> str:
        """
        Сериализует ключ как атрибут Fluent.

        Returns:
            Строка в формате атрибута (например, ".desc = Значение").
        """
        return f".{self.get_key_last_name(self.key_name)} = {self.get_translation('ru').data['translation']}"

    def serialize_message(self) -> str:
        """
        Сериализует ключ как сообщение Fluent.

        Returns:
            Строка в формате сообщения (например, "key = Значение").
        """
        return f"{self.get_key_last_name(self.key_name)} = {self.get_translation('ru').data['translation']}"

    def get_translation(self, language_iso: str = 'ru') -> LokaliseTranslation:
        """
        Получает перевод для указанного языка.

        Args:
            language_iso: Код языка (по умолчанию 'ru').

        Returns:
            Объект LokaliseTranslation с данными перевода.

        Raises:
            IndexError: Если перевод для языка не найден.
        """
        translations = [t for t in self.data.translations if t['language_iso'] == language_iso]
        if not translations:
            raise IndexError(f"Перевод для языка '{language_iso}' не найден для ключа {self.key_name}")
        return LokaliseTranslation(translations[0], self.key_name)


# Пример использования
if __name__ == "__main__":
    # Мок-данные для тестирования
    class MockKeyData:
        key_name = {'web': "Space::Item.desc"}
        translations = [{'language_iso': 'ru', 'translation': "Описание"}]

    key = LokaliseKey(MockKeyData())
    print(f"Key name: {key.key_name}")
    print(f"Is attr: {key.is_attr}")
    print(f"Parent key: {key.get_parent_key()}")
    print(f"Serialized: {key.serialize()}")
    print(f"File paths: ru={key.get_file_path().ru}, en={key.get_file_path().en}")