# LangFileCleaner

A tool written in C# used to help translators fix/sync their lang doc quickly and smoothly

## Usage

### Check Lang file

This command will check if a Lang file has any unused resource key.

```bash
./LangFileCleaner.exe unused -r "[PROJ_ROOT]" -f "[LANG_FILE_RELATIVE_PATH]" -fu -v
```

- `-fu`: When passing this flag, program will return non-zero exit code if there are any unused keys detected in the Lang file.
- `-v`: When passing this flag, console will write the DEBUG logs.

### Repair Lang file

This command will first scan unused keys in the Lang file and comment them out, then it will write the new result to the destination.

```bash
./LangFileCleaner.exe repair -r "[PROJ_ROOT]" -f "[LANG_FILE_RELATIVE_PATH]" -o "[OUT_PATH]" -v
```

- `-v`: When passing this flag, console will write the DEBUG logs.

### Sync Lang file

This command will using the source file to sync all the missing resource into the target Lang file.

```bash
./LangFileCleaner.exe sync -s "[SOURCE_FILE_PATH]" -t "[TARGET_FILE_PATH]" -o "[OUT_PATH]" -v
```

- `-v`: When passing this flag, console will write the DEBUG logs.
