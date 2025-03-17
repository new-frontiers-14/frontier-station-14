import typing
from pydash import py_
import lokalise
from lokalisemodels import LokaliseKey


class LokaliseProject:
    def __init__(self, project_id: str, personal_token: str):
        """
        Инициализирует объект для работы с проектом Lokalise.

        Args:
            project_id: Идентификатор проекта Lokalise.
            personal_token: Персональный токен для аутентификации.
        """
        self.project_id = project_id
        self.personal_token = personal_token
        self.client = lokalise.Client(self.personal_token)

    def get_all_keys(self) -> typing.List[LokaliseKey]:
        """
        Получает все ключи перевода из проекта Lokalise, сортируя их по времени изменения.

        Returns:
            Список объектов LokaliseKey, отсортированных по убыванию времени изменения.
        """
        keys_items: typing.List[lokalise.client.KeyModel] = []
        page = 1

        while True:
            try:
                keys = self.get_keys(page)
                keys_items.extend(keys.items)  # Используем extend для добавления элементов
                if len(keys_items) >= keys.total_count:
                    break
                page += 1
            except lokalise.errors.LokaliseError as e:
                raise RuntimeError(f"Ошибка получения ключей на странице {page}: {e}") from e

        # Сортировка по убыванию времени изменения
        sorted_list = sorted(
            keys_items,
            key=lambda item: item.translations_modified_at_timestamp or 0,  # Запасное значение для None
            reverse=True
        )
        return [LokaliseKey(k) for k in sorted_list]

    def get_keys(self, page: int) -> lokalise.client.KeysCollection:
        """
        Получает страницу ключей из проекта Lokalise.

        Args:
            page: Номер страницы для загрузки.

        Returns:
            Коллекция ключей с переводами.
        """
        return self.client.keys(
            self.project_id,
            {'page': page, 'limit': 5000, 'include_translations': 1}
        )


# Пример использования
if __name__ == "__main__":
    project_id = "your_project_id"
    token = "your_personal_token"
    lokalise_proj = LokaliseProject(project_id, token)
    try:
        keys = lokalise_proj.get_all_keys()
        print(f"Получено ключей: {len(keys)}")
        for key in keys[:5]:  # Первые 5 для примера
            print(f"Ключ: {key.key_name}, Последнее изменение: {key.translations_modified_at_timestamp}")
    except Exception as e:
        print(f"Ошибка: {e}")