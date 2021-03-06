﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using SAOD_Kursovoy.Service;
using SAOD_Kursovoy.Model.Elements;

namespace SAOD_Kursovoy.Model
{
    /// <summary>
    /// Реализует структуру хеш-таблицы.
    /// </summary>
    class HashTable<T> : IEnumerable<T>, INotifyCollectionChanged
    {
        private const ushort _sizeSegments = 1024;  // Размер сегмента
        private const byte _c = 2;  // Константы для квадратичного опробования: 
        private const byte _d = 3;  // адрес = h(x) + c·i + d·i^2

        private byte _countSegments = 1;    // Количество сегментов
        private HashElements<T>[] _array;   // Массив элементов

        private readonly Func<string, ushort> _getHash; // Функция расчета хеш-значения

        /// <summary>
        /// Хеш-таблица.
        /// </summary>
        /// <param name="getHash">Функция для расчета хеш-значения.</param>
        public HashTable(Func<string, ushort> getHash)
        {
            _array = new HashElements<T>[_sizeSegments];
            _getHash = getHash;
            OnCollectionChanged();
        }

        /// <summary>
        /// Возвращает индекс на элемент с заданным ключом.
        /// В случае неудачи возвращает индекс на первый null.
        /// </summary>
        /// <param name="key">Ключевое значения для хеш-функции.</param>
        private uint GetIndex(string key)
        {
            uint hash = _getHash(key);  // Получение хеш-значения
            uint _startHash = hash;     // Сохранение начального значения
            uint i = 1;                 // Отслеживание количество попыток
            Log.Add($"Для ключа {key} получено значение {hash}");

            // Поиск элемента по ключу
            while (_array[hash] != null && _array[hash]?.Key != key)
            {
                i++; // Увелечение значения попыток
                // Расчет по квадратичному опробованию: адрес = h(x) + c·i + d·i^2
                hash = Convert.ToUInt32(_getHash(key) + _c * i + _d * i * i);
                Log.Add($"Исправление коллизии для ключа {key}. Новый адрес {hash} = {_getHash(key)} + {_c} * {i} + {_d} * {i}^2");

                // Проверка на выход за диапазон
                if (hash > MaxCount)
                    hash %= MaxCount;
            }

            return hash;    // Возвращение индекса
        }

        // Увеличивает размер таблицы
        private void ResizeTable()
        {
            _countSegments++;       // Увеличение количества сегментов        
            var oldArray = _array;  // Сохранение текущего массива
            _array = new HashElements<T>[MaxCount]; // Создание нового массива

            // Копирование элементов
            for (uint i = 0; i < oldArray.Length; i++)
                if (oldArray[i] != null)
                {
                    // Получение нового индекса для старого ключа
                    uint j = GetIndex(oldArray[i].Key);
                    _array[j] = oldArray[i];    // Запись в новый массив
                    oldArray[i] = null;         // Обнуление указателя в старом массиве
                }
        }

        private uint _count = 0;
        /// <summary>
        /// Количество элементов в таблице.
        /// </summary>
        public uint Count
        {
            get { return _count; }
            set { _count = value; }
        }

        /// <summary>
        /// Максимальное количество элементов в таблице.
        /// </summary>
        public uint MaxCount
        {
            get { return (uint)_sizeSegments * _countSegments; }
        }

        /// <summary>
        /// Возвращает значение, связанное с ключом хеш-таблицы.
        /// </summary>
        /// <param name="key">Ключевое значение.</param>
        public T Find(string key)
        {
            var el = _array[GetIndex(key)];
            return (el != null) ? el.Value : default(T);
        }

        /// <summary>
        /// Добавляет элемент в хеш-таблицу.
        /// </summary>
        /// <param name="key">Ключевое значения для хеш-функции.</param>
        /// <param name="value">Добавляемое значение.</param>
        public void Add(string key, T value)
        {
            // Проверка, что заполненность хеш-таблицы превысила 50%
            if (_count > MaxCount / 2)
                ResizeTable();      // Увеличить размер таблицы

            // Получение индекса на новый элемент
            uint i = GetIndex(key);
            if (_array[i] != null && !_array[i].IsDelete)
                throw new Exception("Ошибка при добавлении. Ячейка уже занята!");

            //Создание нового объекта
            _array[i] = new HashElements<T>()
            {
                Hash = _getHash(key),
                Key = key,
                IsDelete = false,
                Value = value
            };

            // Сохранение сообщения в журнале
            Log.Add($"Добавлен объект \"{value}\".\nРазмещение по индексу {i}.");
            _count++;   // Увеличение количества

            // Оповещение об изменении коллекции
            OnCollectionChanged();
        }

        /// <summary>
        /// Помечает элемент как удаленный.
        /// </summary>
        /// <param name="key">Ключевое значения для хеш-функции.</param>
        public void Delete(string key)
        {
            // Получение индекса на элемент
            uint i = GetIndex(key);

            // Отметка его на удаление
            if (_array[i] != null && !_array[i].IsDelete)
                _array[i].IsDelete = true;
            else
                throw new Exception("Ошибка при удалении. Элемент не существует!");

            // Сохранение сообщения в журнале
            Log.Add($"Удален объект \"{_array[i].Value}\".\nРазмещение по индексу {i}.");
            _count--;   // Уменьшение количества

            // Оповещение об изменении коллекции
            OnCollectionChanged();
        }

        /// <summary>
        /// Очищает хеш-таблицу от элементов.
        /// </summary>
        public void Clear()
        {
            // Обнуление всех элементов
            for (uint i = 0; i < _array.Length; i++)
                if (_array[i] != null)
                      _array[i] = null;

            // Установка значений по умолчанию
            _countSegments = 1;
            _array = new HashElements<T>[_sizeSegments];
            _count = 0; // Обнуление количества

            // Сохранение сообщения в журнале
            Log.Add($"Список хеш-таблицы очищен.");

            // Оповещение об изменении коллекции
            OnCollectionChanged();
        }

        /// <summary>
        /// Возвращает перечислитель для хеш-таблицы. 
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new HashTableEnumerator<T>(_array);
        }

        /// <summary>
        /// Возвращает перечислитель для хеш-таблицы. 
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return new HashTableEnumerator<T>(_array);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        /// <summary>
        /// Оповещает об изменении в коллекции.
        /// </summary>
        public void OnCollectionChanged()
        {
            // Параметр Reset используется, чтобы сохранить порядок элементов коллекции
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// Перечислитель для элементов хеш-таблицы.
    /// </summary>
    class HashTableEnumerator<T> : IEnumerator<T>
    {
        private HashElements<T>[] _array; // Массив элементов
        private uint i;  // Указывает позицию текущего элемента

        // Конструктор для получения массива
        public HashTableEnumerator(HashElements<T>[] array)
        {
            _array = array;
        }

        public T Current => _array[i].Value; // Текущий элемент

        object IEnumerator.Current => _array[i].Value; // Текущий элемент

        // Перемещает перечислитель к следующему элементу коллекции
        public bool MoveNext()
        {
            i++;
            // Пока элемент пустой 
            while (i < _array.Length && (_array[i] == null || _array[i].IsDelete))
                i++; // Увеличиваем индекс
            
            return i < _array.Length; // Проверка, что не вышли за границу
        }

        // Установка перечислителя в начальное положение
        public void Reset()
        {
            i = uint.MaxValue;
        }

        public void Dispose()
        {
        }
    }
}
