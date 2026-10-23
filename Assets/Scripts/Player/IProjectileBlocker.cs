using UnityEngine; 
public interface IProjectileBlocker
{
    bool BloqueaProyectiles { get; }
    Collider2D ColliderBloqueador { get; }
}