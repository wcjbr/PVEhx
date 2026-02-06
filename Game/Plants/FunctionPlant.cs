using Godot;

/// <summary>
/// 功能类植物基类
/// </summary>
public partial class FunctionPlant : Plant
{
    [Export]
    public string FunctionType = "None";
    public override void _Ready()
    {
        base._Ready();
    }
    public override void _Process(double delta)
    {

        base._Process(delta);
        Hooks(delta);
    }

    public virtual void Hooks(double delta)
    {
        // 功能类植物的钩子方法，可以在子类中实现具体功能
        return;
    }
}