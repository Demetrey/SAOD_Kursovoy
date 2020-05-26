﻿namespace SAOD_Kursovoy.Model.Elements
{
    class HashElements<T>
    {
		/// <summary>
		/// Хеш-значение. 
		/// </summary>
		public ushort Hash { get; set; }

		/// <summary>
		/// Ключевое значения для хеш-функции.
		/// </summary>
		public string Key { get; set; }

		private bool _isDelete;
		/// <summary>
		/// Определяет помечен ли элемент на удалание.
		/// </summary>
		public bool IsDelete { get; set; }

		/// <summary>
		/// Определяет значение, которое хранит элемент.
		/// </summary>
		public T Value { get; set; }
	}
}
