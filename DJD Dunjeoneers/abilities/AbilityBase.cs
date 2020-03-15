using Godot;
using System;

public abstract class AbilityBase : Node2D{
    protected Vector2 direction = default(Vector2);
    protected Vector2 position = default(Vector2);
    protected int targetLayer = -1;
    protected float knockbackStrength = 50f;
    protected int maxPierces = 0;
    protected int pierces = 0;
    protected float damage = 25f;
    protected float velocity = 30f;
    protected float maxVelocity = 100f;
    protected float acceleration = 30f;
    protected float effectRadius = 0f;
    public float cooldown = 0.5f;

    public AbilityBase(){}

    public AbilityBase(Vector2 _direction, Vector2 _position, int _targetLayer){
        direction = _direction;
        Position = _position;
        targetLayer = _targetLayer;
    }
}
