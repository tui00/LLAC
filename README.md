# LLAC
[![Computer](https://img.shields.io/badge/logic--arrows-map-blue)](https://logic-arrows.io/map-computer)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/tui00/LLAC/dotnet.yml)](https://github.com/tui00/LLAC/actions/workflows/dotnet.yml)
[![License](https://img.shields.io/github/license/tui00/LLAC)](https://github.com/tui00/LLAC/blob/main/LICENSE)
[![Last release version](https://img.shields.io/github/v/release/tui00/LLAC?include_prereleases)](https://github.com/tui00/LLAC/releases)
[![Static Badge](https://img.shields.io/badge/English-README-red)](https://github-com.translate.goog/tui00/LLAC/blob/main/README.md?_x_tr_sl=ru&_x_tr_tl=en&_x_tr_hl=ru&_x_tr_pto=wapp)

## Краткое описание
Это мини-преобразователь простых команд в сложные конструкции для asembler-а [компьютера](https://logic-arrows.io/map-computer) **второй версии** из игры [logic-arrows](https://logic-arrows/)

## Этот проект еще не готов!
Если я захочу, я буду менять имя, аргументы и что делают команды хоть каждый день.

## Скачивание проекта
> **⚠️ Этими способами вы можете скачать только `Debug` версию. Если нужна `Release` версия, перейдите во вкладку [`Releases`](https://github.com/tui00/LLAC/releases)**
- С помощью workflow:  
  - Откройте вкладку `Actions`
  - Выберите `.NET`
  - Выберите последний запуск
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
readchar a ; Не изменяет регистры
```
a -- Это регистер куда будет помещена считаная клавиша

### Терминал
```asm
@connect term

ldi a, "H"
writechar a ; Вывод одиночного символа, не трогает регистры

writeline hello ; Вывод null-terminated строки, изменяет a, b и d
hello db "Hello, World!", 0
```

### Дисплей
```asm
@connect coldisp

drawimage img ; Вывод изображения на экран, изменяет a, b, c и d

cleardisp ; Очистка экрана, изменяет a, c и d, не изменяет регистры

exit

image img, img.bmp
```
Вы также можете вывести изображение на дисплей во время загрузки дискеты
```asm
@connect coldisp ; Важно использовать @connect, а не connect
@image preRunImage.bmp
```
Желательно что-бы изображение:
1. было 16x16 или больше пикселей
2. в формате bmp
3. не содержало зеленого компонента(он просто [не поддерживается](https://github.com/chubrik/LogicArrows/blob/main/computer-v2/specification.md#дисплей))
4. содержало альфа канал(можно просто заполнить не использованое пространство черным)

Но все это кроме 1 можно не соблюдать

### Счетчик
```asm
@connect digit ; Подключаем в без-знаковом режиме

prepare 1234, a:b ; Подготавливаем число 1234 для вывода
writenum a:b ; Выводим, не изменяет регистры

cleardigit ; Очищаем, изменяет a
```

### Другое
Вы можете загрузить в [порт для выбора устройств(0x3E)](https://github.com/chubrik/LogicArrows/blob/main/computer-v2/specification.md#оперативная-память) значение
```asm
@connect signdigit, coldisp ; Загрузить во время загрузки дискеты в память

connect term, disp, digit ; Загрузить во время выполнения программы, изменяет a
```

### Команды-алиасы
```asm
exit ; Эквивалентно hlt
string hello, "Hello, World!" ; Эта команда создают null-terminated строку
image img, img.bmp ; Команда для создания изображения
```

### Конец
На этом пока все в будущем будет больше функций

## Roadmap
- [x] Добавить возможность вывода символов в терминал
- [x] Добавить вывод на цифровой экран
- [x] Добавить вывод на дисплей в монохромном режиме
- [x] Добавить вывод на дисплей в цветном режиме
- [ ] Сделать авто переключение между [банками памяти](https://github.com/chubrik/LogicArrows/blob/main/computer-v2/specification.md#оперативная-память) при выполнении кода

## P. S.
Я не умею писать нормальные README
