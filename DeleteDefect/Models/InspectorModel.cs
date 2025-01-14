using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class InspectorModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-increment
    public String Id { get; set; }

    required public string PasswordHash { get; set; }
    required public string NIK { get; set; }
    required public string Name { get; set; }
}
