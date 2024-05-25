import os
import subprocess
import tkinter as tk
from tkinter import simpledialog

def file_contains_string(file_path, search_string):
    with open(file_path, 'r', encoding='utf-8') as file:
        for line in file:
            if search_string in line:
                return True
    return False

def search_files(search_string, directory_path):
    notepad_plus_plus_path = r"C:\Program Files\Notepad++\notepad++.exe"  # Укажите полный путь к notepad++.exe
    for root, _, files in os.walk(directory_path):
        for file in files:
            if file.endswith(".ftl"):  # проверяем только файлы с расширением .ftl
                file_path = os.path.join(root, file)
                if file_contains_string(file_path, search_string):
                    # Открываем файл в Notepad++
                    subprocess.run([notepad_plus_plus_path, file_path])

def main():
    # Путь к директории, в которой нужно искать файлы
    directory_path = r"D:\OtherGames\SpaceStation14\перевод\corvax-frontier-14\Resources\Locale"

    # Создание графического интерфейса
    root = tk.Tk()
    root.withdraw()  # Скрыть основное окно

    # Запрос ввода строки для поиска
    search_string = simpledialog.askstring("Input", "Введите строку для поиска:")

    if search_string:
        search_files(search_string, directory_path)
    else:
        print("Поисковая строка не введена.")

if __name__ == "__main__":
    main()
