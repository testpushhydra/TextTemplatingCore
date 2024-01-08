//---------------------------------------------//
// Copyright 2022 CloudIDEaaS                        //
// https://github.com/CloudIDEaaS/TextTemplatingCore //
//---------------------------------------------//
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CloudIDEaaS.TextTemplatingCore.TextTemplatingCoreLib
{
    public sealed class TemplateException : Exception
    {
        public ImmutableArray<TemplateError> Errors { get; }

        public TemplateException(params TemplateError[] errors)
            : this(errors.AsEnumerable())
        {
        }

        public TemplateException(IEnumerable<TemplateError> errors)
        {
            Errors = errors.ToImmutableArray();
        }
    }
}
