from typing import Optional, List, Dict, Any


class YAMLEntity:
    def __init__(self, id: str, name: Optional[str], description: Optional[str], 
                 suffix: Optional[str], parent_id: Optional[str] = None):
        """
        Представляет сущность из YAML с основными атрибутами.

        Args:
            id: Уникальный идентификатор сущности.
            name: Название сущности (может быть None).
            description: Описание сущности (может быть None).
            suffix: Суффикс сущности (может быть None).
            parent_id: Идентификатор родительской сущности (по умолчанию None).
        """
        self.id = id
        self.name = name
        self.description = description
        self.suffix = suffix
        self.parent_id = parent_id


class YAMLElements:
    def __init__(self, items: List[Dict[str, Any]]):
        """
        Преобразует список YAML-элементов в список объектов YAMLEntity.

        Args:
            items: Список словарей, представляющих YAML-элементы.
        """
        self.elements = [self.create_element(item) for item in items if item is not None]

    def create_element(self, item: Dict[str, Any]) -> Optional[YAMLEntity]:
        """
        Создаёт объект YAMLEntity из словаря YAML-данных.

        Args:
            item: Словарь с данными элемента YAML.

        Returns:
            Объект YAMLEntity или None, если элемент невалиден или не является сущностью.
        """
        if not isinstance(item, dict) or 'id' not in item:
            return None

        if item.get('type') == 'entity':
            return YAMLEntity(
                id=item['id'],
                name=item.get('name'),
                description=item.get('description'),
                suffix=item.get('suffix'),
                parent_id=item.get('parent')
            )
        return None


# Пример использования
if __name__ == "__main__":
    # Тестовые данные
    yaml_data = [
        {'id': 'ent1', 'type': 'entity', 'name': 'Item', 'description': 'A test item'},
        {'id': 'ent2', 'type': 'entity', 'parent': 'ent1'},
        {'type': 'other'},  # Некорректный элемент
        {'id': 'ent3'}  # Минимальный элемент
    ]

    elements = YAMLElements(yaml_data)
    for el in elements.elements:
        if el:
            print(f"ID: {el.id}, Name: {el.name}, Desc: {el.description}, Suffix: {el.suffix}, Parent: {el.parent_id}")