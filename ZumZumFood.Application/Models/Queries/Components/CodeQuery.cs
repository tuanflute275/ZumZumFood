﻿namespace ZumZumFood.Application.Models.Queries.Components
{
    public class CodeQuery : BaseQuery<Code>
    {
        public string Keyword { get; set; }
    }
}
