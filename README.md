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
Еще вы можете вывести изображение на дисплей во время загрузки дискеты
```asm
@connect coldisp ; Выжно использовать @connect, а не connect
@image preRunImage.bmp
```
Изображение должно быть в формате bmp размером 16 на 16 и содержать только эти цвета(в формате 0xRRGGBBAA): 0xFF000000, 0x0000FF00, 0xFF00FF00, 0x00000000.

### Счетчик
```asm
@connect digit ; Подключаем в без-знаковом режиме

prepare 1234, a:b ; Подготавливаем число 1234 для вывода
writenum a:b ; Выводим, не изменяет регистры

cleardigit ; Очищяем, изменяет a
```

### Другое
Вы можете загрузить в порт для выбора устройств(0x3E) значение
```asm
@connect signdigit, coldisp ; Загрузить во время загрузки дискеты в память

connect term, disp, digit ; Загрузить во время выполнения програмы, изменяет a
```

### Команды-алиасы
```asm
exit ; Эквивалентно hlt
string hello, "Hello, World!" ; Эта команда создают null-terminated строку
image img, img.bmp ; Команда для создания изображения
```

### Конец
На этом пока всю в будущем будет больше функций

## Roadmap
- [x] Добавить возможность вывода символов в терминал
- [x] Добавить вывод на цифровой экран
- [x] Добавить вывод на дисплей в монохромном режиме
- [x] Добавить вывод на дисплей в цветном режиме
- [ ] Сделать авто переключение между банками памяти при выполнении кода

## P. S.
Я не умею писать нормальные README. Извините
