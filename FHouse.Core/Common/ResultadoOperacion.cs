using System.Collections.Generic;

namespace FHouse.Core.Common
{
    public class ResultadoOperacion
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; }
        public List<string> Errores { get; set; } = new List<string>();
        public object Datos { get; set; }

        public static ResultadoOperacion Ok(string mensaje = "Operación realizada con éxito.", object datos = null)
        {
            return new ResultadoOperacion
            {
                Exitoso = true,
                Mensaje = mensaje,
                Datos = datos
            };
        }

        public static ResultadoOperacion Falla(string error)
        {
            return new ResultadoOperacion
            {
                Exitoso = false,
                Mensaje = error,
                Errores = new List<string> { error }
            };
        }

        public static ResultadoOperacion Falla(IEnumerable<string> errores, string mensaje = "Ocurrieron errores de validación.")
        {
            return new ResultadoOperacion
            {
                Exitoso = false,
                Mensaje = mensaje,
                Errores = new List<string>(errores)
            };
        }
    }

    public class ResultadoOperacion<T>
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; }
        public List<string> Errores { get; set; } = new List<string>();
        public T Datos { get; set; }

        public static ResultadoOperacion<T> Ok(T datos, string mensaje = "Operación exitosa.")
        {
            return new ResultadoOperacion<T>
            {
                Exitoso = true,
                Mensaje = mensaje,
                Datos = datos
            };
        }

        public static ResultadoOperacion<T> Falla(string error)
        {
            return new ResultadoOperacion<T>
            {
                Exitoso = false,
                Mensaje = error,
                Errores = new List<string> { error }
            };
        }

        public static ResultadoOperacion<T> Falla(IEnumerable<string> errores, string mensaje = "Errores de validación.")
        {
            return new ResultadoOperacion<T>
            {
                Exitoso = false,
                Mensaje = mensaje,
                Errores = new List<string>(errores)
            };
        }
    }
}
