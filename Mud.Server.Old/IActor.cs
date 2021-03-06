﻿namespace Mud.Server.Old
{
    // IActor can process command and receive message
    public interface IActor
    {
        bool ProcessCommand(string commandLine);
        void Send(string message);
    }
}
