using Godot;
using System;

public abstract class Entity : KinematicBody2D{
    [Signal] public delegate float HealthChanged();
    [Signal] public delegate float EnergyChanged();
    [Signal] public delegate Entity Dead();


    public bool IsAlive {
        get{ return CurHealth > 0; }
        set{
            if (CurHealth > 0 == value) return;
            if (CurHealth > 0 && !value){
                CurHealth = 0;
                EmitSignal(nameof(Dead), this);
                PrepareDeath();
            }
            else CurHealth = 1f;
        }
    }
    public float MaxHealth {
        get{ return _maxHealth; } 
        set{
            _maxHealth = value;
            healthBar.MaxValue = value;
        }
    }
    public float CurHealth {
        get{ return _curHealth; } 
        set{
            _curHealth = value;
            healthBar.Value = value;
            if (_curHealth < MaxHealth) healthBar.Show();
            EmitSignal(nameof(HealthChanged), _curHealth);
            if (!IsAlive){
                EmitSignal(nameof(Dead), this);
                PrepareDeath();
            }
        }
    }
    public float MaxEnergy {get; set;} = 100f;
    public float CurEnergy {
        get{ return _curEnergy; }
        set{
            _curEnergy = value;
            EmitSignal(nameof(EnergyChanged), _curEnergy);
        }
    }
    public float Acceleration {get; set;} = 270f;
    public float MaxVelocity {get; set;} = 90f;
    public int DeathValue { get; protected set; } = 1;

    private float _curHealth = 100f;
    private float _maxHealth = 100f;
    private float _curEnergy = 100f;

    public Vector2 velocity = new Vector2();
    public AnimatedSprite sprite = new AnimatedSprite();
    public Sprite shadow = new Sprite();

    protected float knockbackDeceleration = 5f;
    protected float knockbackCutoff = 4f;
    protected Vector2 knockbackVelocity = new Vector2();
    protected float knockbackResistance = 0.0f; // 0 .. 1
    protected Tween deathTween = new Tween();
    protected Random rng = new Random();
    protected CollisionShape2D moveCollider = new CollisionShape2D();
    protected TextureProgress healthBar = new TextureProgress();
    protected Globals globals;

    protected int size;

    public virtual void Initialize(Vector2 position, int size = 8){
        Position = position;
        this.size = size;
        deathTween.InterpolateProperty(this, "modulate", new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 0f), 1);
        deathTween.InterpolateCallback(this, 1, nameof(Die));
        AddChild(deathTween);
        RectangleShape2D moveShape = new RectangleShape2D();
        moveCollider.Shape = moveShape;
        moveCollider.Position = new Vector2(0, size/3f);

        if (size == 8){
            shadow.Texture = ResourceLoader.Load("res://assets/sprites/shadow.png") as Texture;
            shadow.Position = new Vector2(0, 1);
            moveShape.Extents = new Vector2(1,2);
            healthBar.TextureUnder = ResourceLoader.Load("res://assets/gradients/RedGradient_6.tres") as Texture;
            healthBar.TextureProgress_ = ResourceLoader.Load("res://assets/gradients/GreenGradient_6.tres") as Texture;
            healthBar.MarginBottom = -5;
            healthBar.MarginTop = -6;
            healthBar.MarginRight = 3;
            healthBar.MarginLeft = -3;
        }
        else if (size == 12){
            shadow.Texture = ResourceLoader.Load("res://assets/sprites/shadow_12.png") as Texture;
            shadow.Position = new Vector2(0, 2);
            moveShape.Extents = new Vector2(3, 2);
            healthBar.TextureUnder = ResourceLoader.Load("res://assets/gradients/RedGradient_12.tres") as Texture;
            healthBar.TextureProgress_ = ResourceLoader.Load("res://assets/gradients/GreenGradient_12.tres") as Texture;
            healthBar.MarginBottom = -7;
            healthBar.MarginTop = -8;
            healthBar.MarginRight = 7;
            healthBar.MarginLeft = -7;
        }
        else if (size == 24){
            shadow.Texture = ResourceLoader.Load("res://assets/sprites/shadow_24.png") as Texture;
            shadow.Position = new Vector2(0, 4);
            moveShape.Extents = new Vector2(5, 3);
            healthBar.TextureUnder = ResourceLoader.Load("res://assets/gradients/RedGradient_16.tres") as Texture;
            healthBar.TextureProgress_ = ResourceLoader.Load("res://assets/gradients/GreenGradient_16.tres") as Texture;
            healthBar.MarginBottom = -13;
            healthBar.MarginTop = -14;
            healthBar.MarginRight = 8;
            healthBar.MarginLeft = -8;
        }
        shadow.ZIndex = -1;
        healthBar.RectPosition = new Vector2(healthBar.MarginLeft, -size/2 - 1);
        healthBar.Hide();

        SetCollisionLayerBit(0, false);
        SetCollisionMaskBit(0, false);
        SetCollisionMaskBit(1, true);

        AddChild(sprite);
        AddChild(healthBar);
        AddChild(moveCollider);
        AddChild(shadow);
    }

    public override void _Ready(){
        globals = GetTree().Root.GetNode("Globals") as Globals;
    }

    public override void _Process(float delta){
        ZIndex = Mathf.FloorToInt(Position.y);
    }

    public override void _PhysicsProcess(float delta){
        ProcessKnockback(delta);
    }

    protected void ProcessKnockback(float delta){
        if (knockbackVelocity.Length() > knockbackCutoff){
            float lerpWeight = knockbackDeceleration * delta;
            knockbackVelocity.x = Mathf.Lerp(knockbackVelocity.x, 0.0f, lerpWeight);
            knockbackVelocity.y = Mathf.Lerp(knockbackVelocity.y, 0.0f, lerpWeight);
            Vector2 motion = knockbackVelocity * delta;
            KinematicCollision2D result = MoveAndCollide(motion);
            if (IsInstanceValid(result)) MoveAndCollide(result.Normal.Slide(motion));
        }
    }

    protected virtual void PrepareDeath(){
        sprite.Stop();
        moveCollider.QueueFree();
        CollisionMask = 0;
        CollisionLayer = 0;
        deathTween.Start();
    }

    protected virtual void Die(){
        QueueFree();
    }

    public virtual void Damage(float damage, Vector2 knockback = new Vector2()){
        CurHealth = Mathf.Max(CurHealth - damage, 0);
        knockbackVelocity += knockback * (1 - knockbackResistance);
    }

    protected Vector2 RandomDirection(){
        return new Vector2((float)rng.NextDouble() * 2 - 1, (float)rng.NextDouble() * 2 - 1);
    }

}
