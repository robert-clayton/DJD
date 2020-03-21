using Godot;
using System;

public class Pyroblast : Fireball{

    public Pyroblast() : base(){}

    public Pyroblast(Vector2 direction, Vector2 pos, int targetLayer) : base(direction, pos, targetLayer){
        maxPierces = 3; 
        damage = 1000f;
        knockbackStrength = 200f;
        Acceleration *= .5f;
        LifetimeTotal *= 2f;
        effectRadius = 50f;
        cooldown = 3f;
        Scale = Scale * 2f;
    }

    public override void _OnHurtAreaEnter(Area2D hurtArea){
        if (++pierces >= maxPierces){
            base._OnHurtAreaEnter(hurtArea);
        } else DamageNearby();
    }
}
