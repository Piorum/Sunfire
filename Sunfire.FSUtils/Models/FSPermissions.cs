
namespace Sunfire.FSUtils.Models
{
    public class FSPermissions
    {
        public bool UserRead { get; set; }
        public bool UserWrite { get; set; }
        public bool UserExecute { get; set; }

        public bool GroupRead { get; set; }
        public bool GroupWrite { get; set; }
        public bool GroupExecute { get; set; }

        public bool OtherRead { get; set; }
        public bool OtherWrite { get; set; }
        public bool OtherExecute { get; set; }
    }
}
