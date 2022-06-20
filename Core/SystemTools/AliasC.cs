using System.Collections.Generic;

namespace Core.SystemTools
{
    public class AliasC
    {
        public string CommandName { get; set; }
        public string Command { get; set; }

        public override bool Equals(object obj)
        {
            return obj is AliasC alias &&
                   CommandName == alias.CommandName &&
                   Command == alias.Command;
        }

        public override int GetHashCode()
        {
            int hashCode = -1670917873;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CommandName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Command);
            return hashCode;
        }
    }
}
