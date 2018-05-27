using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SignalRCluster.Internal
{
    

    [DataContract]
    public class DirectoryAction
    {
        [DataMember(Order = 0)]
        public String Hub { get; set; }
        [DataMember(Order = 1)]
        public DirectoryElementAction Action { get; set; }
        [DataMember(Order = 2)]
        public DirectoryElement Element { get; set; }
        [DataMember(Order = 3)]
        public String Item { get; set; }

        public DirectoryAction() { }
        
        public DirectoryAction(DirectoryElementAction action, DirectoryElement element, string item,string hub)
        {
            Action = action;
            Element = element;
            Item = item;
            Hub = hub;
        }

        public static DirectoryAction AddConnection(string hub, string connectionId)
        {
            return new DirectoryAction(
                    DirectoryElementAction.Add,
                    DirectoryElement.Connection,
                    connectionId,
                    hub);
        }

        public static DirectoryAction AddUser(string hub, string userId)
        {
            return new DirectoryAction(
                    DirectoryElementAction.Add,
                    DirectoryElement.User,
                    userId,
                    hub);
        }

        public static DirectoryAction AddGroup(string hub, string groupId)
        {
            return new DirectoryAction(
                    DirectoryElementAction.Add,
                    DirectoryElement.Group,
                    groupId,
                    hub);
        }

        public static DirectoryAction RemoveConnection(string hub, string connectionId)
        {
            return new DirectoryAction(
                    DirectoryElementAction.Remove,
                    DirectoryElement.Connection,
                    connectionId,
                    hub);
        }

        public static DirectoryAction RemoveUser(string hub, string userId)
        {
            return new DirectoryAction(
                    DirectoryElementAction.Remove,
                    DirectoryElement.User,
                    userId,
                    hub);
        }

        public static DirectoryAction RemoveGroup(string hub, string groupId)
        {
            return new DirectoryAction(
                    DirectoryElementAction.Remove,
                    DirectoryElement.Group,
                    groupId,
                    hub);
        }

    }

    [DataContract]
    public class RemoteDirectoryOperation
    {
        [DataMember(Order = 0)]
        public String Node { get; set; }

        [DataMember(Order = 1)]
        public Int32 Sequence { get; set; }

        [DataMember(Order = 2)]
        public DirectoryAction Action { get; set; }

        public RemoteDirectoryOperation() { }

        public RemoteDirectoryOperation(string node,int sequence,DirectoryAction action)
        {
            Node = node;
            Sequence = sequence;
            Action = action;
        }

    }

    public enum DirectoryElementAction : byte
    {
        Add = 1,
        Remove = 2,
    }

    public enum DirectoryElement : byte
    {
        Connection = 1,
        User = 2,
        Group = 3
    }
}
