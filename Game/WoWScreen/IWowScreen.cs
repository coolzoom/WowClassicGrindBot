﻿using SharedLib;

namespace Game
{
    public interface IWowScreen : IColorReader, IRectProvider
    {
        bool Enabled { get; set; }
    }
}
