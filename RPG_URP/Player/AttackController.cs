using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackController : MonoBehaviour
{
    public PlayerController playerController;
    public UIController uiController;
    public VFXController vfxController;
    public BFXController bfxController;
    public AudioController audioController;

    public LayerMask bulletCollisionMask;

    public bool heavyAttackBuffered = false;
    public bool lightAttackBuffered = false;
    public bool rangedAttackBuffered = false;

    public bool fullyLoaded = false; //fully loaded = double capacity, purple flames turn to green, shooting consumes green flames clockwise then purple

    private int maxAmmoCapacity = 6; //Doubled for fully-loaded
    private int remainingAmmo = 6;
    private int remainingFullyLoadedAmmo = 6;

    private int FLShotDamage = 20;
    private int shotDamage = 10;

    private bool primaryGun = true; //Bool for alternating fire

    public bool shootingFullyLoaded = false;

    public Transform bloodSpawnLocation;

    //https://gamedev.stackexchange.com/questions/117423/unity-detect-animations-end/117425

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void FixedUpdate()
    {
        //uiController.activeAmmo = currentShotCapacity;
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

        if (fullyLoaded) {

            //uiController.useOneFullyLoadedAmmo();

            StartCoroutine(fullyLoadedAttackSequence(target));

        }

        else if (remainingAmmo >= 1 && !shootingFullyLoaded)
        {

            Debug.Log($"Performing ranged attack on: {target.name}");

            Enemy instanceOfEnemyClass = target.GetComponent<Enemy>();

            remainingAmmo--;
            uiController.updateAmmoCounter(remainingAmmo); //Can decouple this later

            shootNormalBulletAtTarget(instanceOfEnemyClass, shotDamage, primaryGun);

        }

    }

    public void setFullyLoaded(bool value) {

        if (value)
        {
            fullyLoaded = true;
            remainingAmmo = maxAmmoCapacity;
            remainingFullyLoadedAmmo = remainingAmmo;
            uiController.setFullyLoaded(true);
        }
        else
        {
            fullyLoaded = false;
            uiController.setFullyLoaded(false);
        }
    
    }

    //Messy bullet handling, refactor later
    public IEnumerator fullyLoadedAttackSequence(GameObject target) {

        shootingFullyLoaded = true;

        Enemy instanceOfEnemyClass = target.GetComponent<Enemy>();

        while (remainingFullyLoadedAmmo > 0) {

            //Play shoot animation + fx

            remainingFullyLoadedAmmo--;
            uiController.useOneFullyLoadedAmmo();
            instanceOfEnemyClass.takeDamage(FLShotDamage);
            vfxController.shootPrimaryGun();
            audioController.playGunshot();

            RaycastHit hit;

            if (Physics.Raycast(transform.position, (target.transform.position - transform.position).normalized, out hit, 500f, bulletCollisionMask))
            {

                bfxController.spawnBloodAtRaycastHit(hit, 5.0f);

            }

            yield return new WaitForSeconds(0.25f);
        }

        fullyLoaded = false;

        while (remainingAmmo > 0) {

            remainingAmmo--;
            uiController.updateAmmoCounter(remainingAmmo);
            shootNormalBulletAtTarget(instanceOfEnemyClass, shotDamage, true);

            yield return new WaitForSeconds(0.25f);

        }

        shootingFullyLoaded = false;

        //yield return null;
    }

    private void shootNormalBulletAtTarget(Enemy target, float damage, bool primaryGun) {

        RaycastHit hit;

        if (Physics.Raycast(transform.position, (target.transform.position - transform.position).normalized, out hit, 500f, bulletCollisionMask)) {

            bfxController.spawnBloodAtRaycastHit(hit, 3.0f);

        }

        target.takeDamage(damage);
        vfxController.shootPrimaryGun();
        audioController.playGunshot();

    }

}
