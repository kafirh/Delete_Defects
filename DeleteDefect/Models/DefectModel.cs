using System.ComponentModel.DataAnnotations.Schema;
using Org.BouncyCastle.Asn1.Cms;

namespace DeleteDefect.Models
{
    [Table("Defect_Results")]
    public class DefectModel
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string ModelCode { get; set; }
        public string SerialNumber { get; set; }
        public int DefectId { get; set; }
        public string InspectorId { get; set; }
        public string ModelNumber { get; set; }
        public int LocationId { get; set; }
        public InspectorModel Inspector { get; set; }
        public LocationModel Location { get; set; }
        public DefectNameModel Defect { get; set; }
    }
}
