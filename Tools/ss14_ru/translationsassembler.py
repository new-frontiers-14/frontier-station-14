import logging
import typing
import os
from fluent.syntax import FluentParser, FluentSerializer
from pydash import py_
from file import FluentFile
from fluentast import FluentSerializedMessage
from lokalise_fluent_ast_comparer_manager import LokaliseFluentAstComparerManager
from lokalise_project import LokaliseProject
from lokalisemodels import LokaliseKey


class TranslationsAssembler:
    def __init__(self, items: typing.List[LokaliseKey]):
        """
        Инициализирует сборщик переводов из ключей Lokalise.

        Args:
            items: Список ключей Lokalise.
        """
        self.group = py_.group_by(items, 'key_base_name')  # Группировка по базовому имени
        self.sorted_keys = sorted(
            self.group.keys(),
            key=lambda key: self._sort_by_translations_timestamp(self.group[key]),
            reverse=True
        )

    def execute(self) -> None:
        """
        Выполняет сборку переводов, обновляя локальные .ftl файлы на основе данных Lokalise.
        """
        for key in self.sorted_keys:
            try:
                full_message = FluentSerializedMessage.from_lokalise_keys(self.group[key])
                parsed_message = FluentParser().parse(full_message)
                ru_full_path = self.group[key][0].get_file_path().ru
                ru_file = FluentFile(ru_full_path)

                try:
                    ru_file_parsed = ru_file.read_parsed_data()
                except FileNotFoundError:
                    logging.error(f"Файл {ru_file.full_path} не существует, создаём новый")
                    ru_file.save_data("")  # Создаём пустой файл
                    ru_file_parsed = FluentParser().parse("")

                manager = LokaliseFluentAstComparerManager(
                    source_parsed=ru_file_parsed, target_parsed=parsed_message
                )

                for_update = manager.for_update()
                for_create = manager.for_create()
                for_delete = manager.for_delete()

                if for_update:
                    updated_ru_file_parsed = manager.update(for_update)
                    updated_ru_file_serialized = FluentSerializer(with_junk=True).serialize(updated_ru_file_parsed)
                    ru_file.save_data(updated_ru_file_serialized)
                    updated_keys = [el.get_id_name() for el in for_update]
                    logging.info(f"Обновлены ключи: {updated_keys} в файле {ru_file.full_path}")

                # TODO: Обработка for_create (добавление новых ключей) и for_delete (удаление лишних)
                if for_create:
                    logging.info(f"Ключи для добавления в Lokalise: {[el.get_id_name() for el in for_create]}")
                if for_delete:
                    logging.info(f"Ключи для удаления из Lokalise: {[el.get_id_name() for el in for_delete]}")

            except Exception as e:
                logging.error(f"Ошибка обработки группы ключей {key}: {e}")

    def _sort_by_translations_timestamp(self, key_list: typing.List[LokaliseKey]) -> int:
        """
        Возвращает временную метку последнего изменения для сортировки.

        Args:
            key_list: Список ключей Lokalise.

        Returns:
            Временная метка последнего изменения (или 0, если отсутствует).
        """
        if not key_list:
            return 0
        sorted_list = sorted(
            key_list,
            key=lambda k: k.data.translations_modified_at_timestamp or 0,
            reverse=True
        )
        return sorted_list[0].data.translations_modified_at_timestamp or 0


# Настройка и выполнение
if __name__ == "__main__":
    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s - %(levelname)s - %(message)s"
    )
    lokalise_project_id = os.getenv('LOKALISE_PROJECT_ID')  # Используем верхний регистр для env
    lokalise_personal_token = os.getenv('LOKALISE_PERSONAL_TOKEN')
    if not lokalise_project_id or not lokalise_personal_token:
        raise ValueError("Не заданы переменные окружения LOKALISE_PROJECT_ID или LOKALISE_PERSONAL_TOKEN")

    lokalise_project = LokaliseProject(
        project_id=lokalise_project_id,
        personal_token=lokalise_personal_token
    )
    all_keys = lokalise_project.get_all_keys()
    translations_assembler = TranslationsAssembler(all_keys)
    translations_assembler.execute()
    logging.info("Сборка переводов завершена.")