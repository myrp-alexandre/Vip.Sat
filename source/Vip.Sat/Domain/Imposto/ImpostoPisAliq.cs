using System.ComponentModel;
using Vip.Sat.DFeCore.Attributes;
using Vip.Sat.DFeCore.Serializer;
using Vip.Sat.Domain.Interfaces;

namespace Vip.Sat.Domain.Imposto
{
    public sealed class ImpostoPisAliq : ICFePis
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        #region Propriedades

        [DFeElement(TipoCampo.Str, "CST", Id = "Q07", Min = 2, Max = 2, Ocorrencia = Ocorrencia.Obrigatoria)]
        public string Cst { get; set; }

        [DFeElement(TipoCampo.De2, "vBC", Id = "Q08", Min = 3, Max = 15, Ocorrencia = Ocorrencia.Obrigatoria)]
        public decimal VBc { get; set; }

        [DFeElement(TipoCampo.De4, "pPIS", Id = "Q09", Min = 5, Max = 5, Ocorrencia = Ocorrencia.Obrigatoria)]
        public decimal PPis { get; set; }

        [DFeElement(TipoCampo.De2, "vPIS", Id = "Q10", Min = 1, Max = 15, Ocorrencia = Ocorrencia.MaiorQueZero)]
        public decimal VPis { get; set; }

        #endregion Propriedades
    }
}