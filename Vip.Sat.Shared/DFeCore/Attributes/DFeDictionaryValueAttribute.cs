using System;
using Vip.Sat.DFeCore.Serializer;

namespace Vip.Sat.DFeCore.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DFeDictionaryValueAttribute : DFeBaseAttribute
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="DFeDictionaryValueAttribute" /> class.
        /// </summary>
        public DFeDictionaryValueAttribute()
        {
            Tipo = TipoCampo.Str;
            Id = "";
            Name = string.Empty;
            Min = 0;
            Max = 0;
            Ocorrencia = 0;
            Descricao = string.Empty;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DFeDictionaryValueAttribute" /> class.
        /// </summary>
        /// <param name="tag">Nome da tag.</param>
        public DFeDictionaryValueAttribute(string tag) : this()
        {
            Name = tag;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DFeDictionaryValueAttribute" /> class.
        /// </summary>
        /// <param name="tipo">The tipo.</param>
        /// <param name="name">The name.</param>
        public DFeDictionaryValueAttribute(TipoCampo tipo, string name) : this()
        {
            Tipo = tipo;
            Name = name;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     Gets or sets the name space.
        /// </summary>
        /// <value>The name space.</value>
        public string Namespace { get; set; }

        #endregion Properties
    }
}