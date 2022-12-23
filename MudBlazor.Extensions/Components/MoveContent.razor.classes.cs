﻿namespace MudBlazor.Extensions.Components;

public enum MoveContentPosition
{
    // Dont rename value names. They are used in MoveContent.js
    BeforeBegin,
    AfterBegin,
    BeforeEnd,
    AfterEnd
}

public enum MoveContentMode
{
    // Dont rename value names. They are used in MoveContent.js
    MoveToSelector, 
    MoveFromSelector
}