using System.Runtime.Serialization;

namespace TLMG.Contracts.Dtos
{
    /// <summary>
    /// Resultado genérico de una operación del servidor.
    /// </summary>
    [DataContract]
    public class OperationResultData
    {
        /// <summary>Indica si la operación se realizó correctamente.</summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>Clave del diccionario de idioma con el motivo del fallo; null si fue exitosa.</summary>
        [DataMember]
        public string ErrorKey { get; set; }
    }
}