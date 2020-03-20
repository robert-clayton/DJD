using Godot;
using System;
using System.Collections.Generic;

public class Boss : Enemy{
    
    public Boss(Vector2 _position, Vector2 _moveAreaStart, Vector2 _moveAreaEnd) : base(_position, _moveAreaStart, _moveAreaEnd, size: 24){
        CurHealth = 1000f;
        MaxHealth = 1000f;
        knockbackResistance = .7f;
    }
}
