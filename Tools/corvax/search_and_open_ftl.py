import os
import fnmatch
import tkinter as tk
from tkinter import simpledialog, messagebox
import subprocess

def search_files(directory, search_text):
    matches = []
    for root, dirnames, filenames in os.walk(directory):
        for filename in fnmatch.filter(filenames, '*.ftl'):
            file_path = os.path.join(root, filename)
            try:
                with open(file_path, 'r', encoding='latin-1') as file:
                    if search_text in file.read():
                        matches.append(file_path)
            except UnicodeDecodeError:
                continue
    return matches

def open_files_in_notepad_plus_plus(files):
    notepad_plus_plus_path = r"C:\Program Files\Notepad++\notepad++.exe"
    for file in files:
        subprocess.Popen([notepad_plus_plus_path, file])

def main():
    directory = r"D:\OtherGames\SpaceStation14\перевод\corvax-frontier-14\Resources\Locale"

    root = tk.Tk()
    root.withdraw()  # Скрываем основное окно

    search_text = simpledialog.askstring("Поиск в файлах", "Введите текст для поиска:")

    if search_text:
        matched_files = search_files(directory, search_text)
        if matched_files:
            open_files_in_notepad_plus_plus(matched_files)
            messagebox.showinfo("Результат поиска", f"Найдено и открыто файлов: {len(matched_files)}")
        else:
            messagebox.showinfo("Результат поиска", "Не найдено файлов, содержащих данный текст.")
    else:
        messagebox.showwarning("Ввод текста", "Вы не ввели текст для поиска.")

if __name__ == "__main__":
    main()
