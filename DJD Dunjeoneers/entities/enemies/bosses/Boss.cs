using Godot;
using System;
using System.Collections.Generic;

public abstract class Boss : Enemy{
    
    protected Boss() : base(size: 24){
        curHealth = 1000f;
        maxHealth = 1000f;
        knockbackResistance = .7f;
    }
}
