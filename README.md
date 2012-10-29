# LENS

## 1. Основные положения


* Встраиваемый скриптовый язык
* Платформа .NET
* Функциональная парадигма, статическая типизация
* Функции - объекты первого рода, алгебраические типы
* Взаимодействие со сборками .NET

## 2. Синтаксис и возможности

Синтаксис максимально лаконичный, близкий к F#, но с существенными упрощениями.
Переменные объявляются словом var, константы словом let.
Блоки выделяются отступами.

### 2.1. Типы данных

В интерпретатор встроена поддержка следующих типов:

* unit = void
* object = System.Object
* bool = System.Boolean
* int = System.Int32
* double = System.Double
* string = System.String

Также поддерживается облегченный синтаксис для:

* массивов: [1, 2, 3]
* кортежей: (1; "hello world")
* лямбда-функций (см. дальше)

### 2.2. Объявление констант и переменных

Позволяют объявить имя для значения или выражения. Константы объявляются
ключевым словом let, переменные - var. Разница очевидна - в константу можно
записать значение только один раз. По сути же, это будет readonly-переменная,
т.е. ее значение может быть каким угодно, полученным на этапе выполнения.

### 2.3. Записи

Запись - то же самое, что структура. Объект, имеющий только поля. Без методов
и модификаторов доступа. Объявляется ключевым словом record:

    record Student
        Name : string
        Age : int
        
Все поля структуры являются публичными.

Структуры могут быть рекурсивными, т.е. включать в себя элементы собственного
типа.

### 2.4. Алгебраические типы

Объявляются ключевым словом type и перечислением возможных ярлыков типа. К
каждому ярлыку может быть прикреплена метка с помощью ключевого слова of:

    type Suit
        | Hearts
        | Clubs
        | Spades
        | Diamonds

    type Card
        | Ace of Suit
        | King of Suit
        | Queen of Suit
        | Jack of Suit
        | ValueCard of Tuple<Suit, int>

Ярлыки должны быть глобально уникальными идентификаторами в контексте скрипта,
поскольку они же являются статическими конструкторами:

    let jack = Jack Hearts
    let two = ValueCard (Diamonds; 2)

### 2.5. Функции

Функции объявляются в теле программы ключевым словом fun:

    fun negate x:int -> -x

    fun hypo a:int b:int ->
        let sq1 = a * a
        let sq2 = b * b
        sqrt (sq1 + sq2)

#### 2.5.1. Аргументы функции

После слова fun идет название функции, а потом ее аргументы с указанием типа.
Если у функции не объявлено ни одного параметра, по сути она все равно будет
принимать тип unit для вызова. Синонимом unit является выражение ().

#### 2.5.2. Возвращаемое значение функции

Любая функция должна возвращать значение. Возвращаемым значением является
последнее выражение тела функции. Если у функции есть несколько выходных точек,
все выражения должны иметь один и тот же тип (типы-наследники не учитываются).
Если функция не может вернуть никакого осмысленного типа, ее последнее
выражение должно быть типа unit.

Тип возвращаемого значения не нужно явно указывать, он может быть выведен
компилятором из типов констант и передаваемых аргументов.

#### 2.5.3 Вызов функции и частичное применение

Функция вызывается, когда ей передаются все требуемые параметры. При передаче
части параметров создается новый объект, являющийся каррированным вариантом
вызываемой функции.

Для того, чтобы вызвать функцию без параметров, ей нужно передать параметр
типа unit - пара скобок ().

    fun sum a:int b:int c:int -> a + b + c
    fun getTen -> 10

    let add1 = sum 1
    let add3 = add1 2
    let five = add3 2
    let alsoFive = sum 1 1 3

    let ten = getTen ()

Вызов функции обладает меньшим приоритетом, чем выражение:

    fun sum a:double b:double -> a + b

    let sum = sqrt sin 1        // sqrt(sin, 1) - wtf?
    let sum = sqrt (sin 1)      // компилируется
    let someData = sum (sin 1) (cos 2)

#### 2.5.4. Анонимные функции

Анонимные функции могут быть объявлены (практически) в любом месте программы.
Помимо наличия имени они отличаются от именованных функций двумя
принципиальными моментами:

1. Анонимная функция замыкает переменные и константы из внешней
   области видимости.
2. Если анонимная функция объявляется при вызове другой функции и у нее
   есть только одна подходящая переопределенная версия, типы аргументов
   можно не указывать.

Анонимная функция описывается следующим образом:

    let sum = (a:int b:int) -> a + b
    let getTen = -> sum 5 5
    let addFive = (a:int) ->
        let b = 5
        sum a b // то же самое, что sum 5
        
Как видно из следующего примера, оператор -> разделяет параметры функции и
ее тело. Даже если параметров нет, 

#### 2.5.5. Чистые функции и мемоизация

При объявлении именованной функции ее можно пометить модификатором pure. Это
означает, что при равных входных параметрах ее результат всегда будет
одинаковым. В таком случае при первом вызове ее результат будет закеширован,
а при повторных вызовах будет использоваться именно этот кеш, а сама функция
не будет повторно вызвана.

Чистота функции не проверяется компилятором. Фактическое наличие побочных
эффектов остается на совести программиста.

#### 2.5.6. Порядок объявления и вызова функций

Порядок не играет роли. Рекурсивные вызовы допустимы без какого-либо явного
указания (например, в F# требуется модификатор rec), взаимная рекурсия
также допустима.

#### 2.5.7. Оператор передачи значения

Для передачи значения в функцию может быть использован оператор `|>`. По сути,
две следующих строки эквивалентны:

        somefx 1
        somefx |> 1
        
Однако этот оператор будет полезен, если аргументы не умещаются на одной строке:

        somefx
        |> value1
        |> (a:int b:int) ->
                let sum = a + b
                sum * sum
        |> (s:string) -> log s

### 2.6. Ключевые слова

#### 2.6.1. Создание объектов

Новые объекты создаются с помощью ключевого слова new:

    let tuple = new Tuple<string, int> "hello" 2

#### 2.6.2. Условие

Условие записывается с помощью блока if / else:
    
    let a = if (1 > 2) 3 else 4

Выражение if, как видно из примера, также возвращает значение, поэтому оба
выражения в его ветках должны быть одного типа.

#### 2.6.3. Цикл

Цикл записывается с помощью блока while:

    var a = 0
    while (a < 10)
       Console.WriteLine "{0} loop iteration" a
       a = a + 1

while также возвращает значение последней строчки его тела на момент
последней итерации.

#### 2.6.4. try-catch

Блоки try-catch записываются следующим образом:

    try
        doSomethingHorrible()
    catch WebException ex
        notify "web exception" ex.Message
    catch DivideByZeroException ex
        notify "whoops!"
    catch
        notify "something weird has happened"

Блок try-catch всегда возвращает unit.

#### 2.6.5. using

Ключевое слово using открывает пространство имен, добавляя объявленные в нем
классы в глобальное:

    using System.Text.RegularExpressions
    let rx = new Regex "[a-z]{2}"

#### 2.6.6. Приведение и проверка типов

Для приведения типов используется оператор as. В отличие от C#, он кидает
InvalidCastException в случае неудачи, а не возвращает null. Может быть
использован на любых типах, в том числе int / string / bool / object.

## 3. Встраиваемость

Технически, интерпретатор будет реализован в виде сборки .NET, которую
программист может подключить к своей программе, чтобы добавить в нее
поддержку скриптового языка.

Сборка содержит основной класс интерпретатора. Схема работы программиста с
интерпретатором следующая:

1. Создать объект интерпретатора
2. Зарегистрировать в интерпретаторе свои типы
3. Передать интерпретатору текст исполняемой программы
  
Примерный код этого взаимодействия на языке C# представлен ниже:

    public class HostMethods
    {
        public void Log(string str)
        {
            Console.WriteLine("{0}: {1}", str, DateTime.Now);
        }
    }
    
    var script = @"HostMethods.Log ""hello world"" ";
    var ipt = new Interpreter();
    ipt.AddHostClass(typeof(HostMethods));
    ipt.AddHostMethod("sum", (int a, int b) => a + b);
    try
    {
        ipt.Run(script);
    }
    catch
    { }
    
## 4. Детали технической реализации

### 4.1. Порядок трансляции

Транслятор работает в три прохода:

1.  Лексический анализ:
    * Удаляются комментарии
2.  Синтаксический анализ:
    * Создается синтаксическое дерево
    * Заполняются список объявленных типов и методов
    * Строится граф замыканий
3.  Генерация исполняемой сборки
    * В среде .NET регистрируется основной класс программы
    * Регистрируются дополнительные объявленные типы и сборки
    * В результате обхода синтаксического дерева генерируется код

## 5. Ограничения

* Нет возможности создавать свои классы с методами, только структуры
* Нет возможности управлять доступом к переменным и методам (private)
* Нет short-circuit операторов (&& и || вычисляют оба операнда)
* Нет цикла foreach, поскольку его можно написать средствами языка
* Нет операторов сокращенного присваивания (+=, ++ и т.д.)
* Нет поддержки событий, обработчиков и делегатов
* Нет цикла с постусловием
* Нет операторов goto, break, continue
* Нет синтаксиса для объявления опциональных параметров в функциях