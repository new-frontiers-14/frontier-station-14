import json
import os
import tkinter as tk
from tkinter import simpledialog
from PIL import Image

# Генерация meta.json
def generate_meta_json(width, height, directions):
    # Собирает все png файлы в папке
    png_files = [f for f in os.listdir('.') if f.endswith('.png')]

    # Создает список states
    states = []
    for file in png_files:
        # Проверяет разрешение изображения
        with Image.open(file) as img:
            img_width, img_height = img.size

        state = {"name": file.replace('.png', '')}
        # Добавляет "directions" только если при запуске их указано >1 и разрешение файла не совпадает с указанным разрешением
        if (img_width != width or img_height != height) and directions > 1:
            state["directions"] = directions

        states.append(state)

    # Генерация заполнения меты
    meta = {
        "version": 1,
        "license": "CC-BY-SA-3.0",
        "copyright": "Python generated",
        "size": {
            "x": width,
            "y": height
        },
        "states": states
    }

    # Сохранение JSON
    with open('meta.json', 'w') as json_file:
        json.dump(meta, json_file, indent=4)

# Интерфейс вся хуйня
def ask_user_input():
    root = tk.Tk()
    root.withdraw()  # Скрыть мусор

    # Спросить за базар
    width = simpledialog.askinteger("Input", "Ширина в пикселях (X):", parent=root, minvalue=1)
    height = simpledialog.askinteger("Input", "Высота в пикселях (Y):", parent=root, minvalue=1)
    directions = simpledialog.askinteger("Input", "Количество направлений:", parent=root, minvalue=0)

    # Закончить с генерацией
    if width is not None and height is not None and directions is not None:
        generate_meta_json(width, height, directions)

ask_user_input()

