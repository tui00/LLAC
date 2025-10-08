# LLAC
[![Computer](https://img.shields.io/badge/logic--arrows-map-blue)](https://logic-arrows.io/map-computer)
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/tui00/LLAC/dotnet.yml)
[![GitHub License](https://img.shields.io/github/license/tui00/LLAC)](https://github.com/tui00/LLAC/blob/main/LICENSE)
![GitHub Release](https://img.shields.io/github/v/release/tui00/LLAC?include_prereleases)

### Краткое описание
Это мини-преобразователь простых команд в сложные конструкции для asembler-а [компьютера](https://logic-arrows.io/map-computer) **второй версии** из игры [logic-arrows](https://logic-arrows/)

## Скачивание проекта
- С помощью workflow:  
  - Откройте вкладку `Actions`
  - Выбирете `.NET`
  - Пролистайте вниз
  - Скачайте нужную версию
- С помощью ссылки:
  - Откройте [ссылку](https://github.com/tui00/LLAC/archive/refs/heads/main.zip)
- С помощью Github CLI:  
  - Запустите `gh repo clone tui00/LLAC`
- С помощью git:  
  - Запустите `git clone https://github.com/tui00/LLAC.git`
- С помощью рук:
  - Откройте раздел `Code`
  - Нажмите `Download ZIP`

## Запуск проекта
> **⚠️ Для запуска установите `.NET 9.0 Runtime`**
- Запустите команду `dotnet build`
- Откройте папку `LLAC/bin/Debug/net9.0`
- Запустите исполняемый файл

## Краткая документация
### Клавиатура
Для того что-бы считать символ с клавиатуры используйте
```asm
readkey a
```
a -- Это регистер куда будет помещена считаная клавиша
### Конец
На этом пока всю в будущем будет больше функций

## Roadmap
- [x] Добавить возможность вывода символов в терминал
- [ ] Добавить вывод на цифровой экран
- [ ] Добавить вывод на дисплей в монохромном режиме
- [ ] Добавить вывод на дисплей в цветном режиме

## P. S.
Я не умею писать нормальные README. Извините
