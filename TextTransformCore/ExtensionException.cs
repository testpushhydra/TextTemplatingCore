//---------------------------------------------//
// Copyright 2022 CloudIDEaaS                        //
// https://github.com/CloudIDEaaS/TextTemplatingCore //
//---------------------------------------------//
using System;

namespace CloudIDEaaS.TextTemplatingCore.TextTransformCore
{
    internal sealed class ExtensionException : Exception
    {
        public ExtensionException()
        {
        }

        public ExtensionException(string message)
            : base(message)
        {
        }
    }
}
