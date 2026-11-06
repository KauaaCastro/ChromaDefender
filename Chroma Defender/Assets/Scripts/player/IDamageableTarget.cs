using UnityEngine;

/// <summary>
/// Interface para qualquer entidade ou objeto que possa receber dano e reagir a ataques de Fogo/Gelo.
/// </summary>
public interface IDamageableTarget
{
    void TakeDamage(int damage, Vector2 knockback, ElementPolarity attackPolarity);
}
