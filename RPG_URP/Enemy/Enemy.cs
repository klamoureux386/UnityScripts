using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(DamageTextController))]
public class Enemy : MonoBehaviour
{
    //Defaults
    public float damage = 10f;
    public float health = 100f;
    public float moveSpeed = 10f;

    private DamageTextController damageTextController;

    private void Awake()
    {
        if (tag != "Enemy")
            Debug.Log($"Object {transform.name} has an Enemy script but is not tagged as an Enemy.");

        damageTextController = GetComponent<DamageTextController>();
    }

    //Default take damage function
    public void takeDamage(float damage) {

        health -= damage;

        damageTextController.createDamageNumberText(damage);

        if (health < 0)
            health = 0;
    }

    //Take Damage with hit location
    public void takeDamage(float damage, Vector3 hitLocation)
    {

        health -= damage;

        damageTextController.createDamageNumberText(damage, hitLocation);

        if (health < 0)
            health = 0;
    }

}
