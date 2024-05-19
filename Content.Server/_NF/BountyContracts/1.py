import os

def gather_cs_files_text(start_directory):
    cs_files_text = []

    for root, dirs, files in os.walk(start_directory):
        for file in files:
            if file.endswith('.cs'):
                full_path = os.path.join(root, file)
                with open(full_path, 'r', encoding='utf-8') as f:
                    file_content = f.read()
                cs_files_text.append(f"File: {full_path}\n{file_content}\n")

    return cs_files_text

def save_to_txt(output_file, content_list):
    with open(output_file, 'w', encoding='utf-8') as f:
        for content in content_list:
            f.write(content)

def main():
    start_directory = os.getcwd()
    output_file = os.path.join(start_directory, 'combined.txt')

    cs_files_text = gather_cs_files_text(start_directory)
    save_to_txt(output_file, cs_files_text)

    print(f"Combined text saved to {output_file}")

if __name__ == "__main__":
    main()
