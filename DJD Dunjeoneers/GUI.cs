using Godot;
using System;

public class GUI : Control{
    public override void _Ready(){
        
        GetNode<TextureButton>("Play").Connect("pressed", this, "_OnPlayPressed");
        GetNode<TextureButton>("Exit").Connect("pressed", this, "_OnExitPressed");
    }

    public void _OnPlayPressed(){
        Engine.TimeScale = 1;
        Hide();
        GetTree().Root.GetNode<Game>("Game").Reset();
    }

    public void _OnExitPressed(){
        GetTree().Quit();
    }

    public override void _Input(InputEvent @event){
        if (Input.IsActionJustPressed("ui_cancel")){
            if (Visible){
                Hide();
                Engine.TimeScale = 1;
                GD.Print(Engine.TimeScale);
            }
            else{
                Show();
                Engine.TimeScale = 0;
            };
        }
    }
}
