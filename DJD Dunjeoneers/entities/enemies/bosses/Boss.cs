using Godot;
using System;
using System.Collections.Generic;

public abstract class Boss : Enemy{
    
    protected Boss(){
        CurHealth = 1000f;
        MaxHealth = 1000f;
        knockbackResistance = .7f;
    }
}
