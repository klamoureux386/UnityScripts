using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackController : MonoBehaviour
{
    public PlayerController playerController;
    public bool heavyAttackBuffered = false;
    public bool lightAttackBuffered = false;
    public bool rangedAttackBuffered = false;

    //https://gamedev.stackexchange.com/questions/117423/unity-detect-animations-end/117425

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    public void startHeavyAttack() {

        heavyAttackBuffered = true;

        //for testing, remove after
        Invoke("unbufferHeavy", 3.0f);

    }

    //for testing, remove after
    private void unbufferHeavy() {

        heavyAttackBuffered = false;
    }

    public void performRangedAttack(GameObject target) {

        Debug.Log($"Performing ranged attack on: {target.name}");

        Enemy instanceOfEnemyClass = target.GetComponent<Enemy>();

        instanceOfEnemyClass.takeDamage(10f);

    }


}
