namespace NitroxClient.Debuggers.Drawer;

public interface IDrawer<T>
{
    public T Draw(T target);
}
