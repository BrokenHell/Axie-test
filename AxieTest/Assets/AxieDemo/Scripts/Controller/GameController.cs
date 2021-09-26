using System.Collections;
using System.Collections.Generic;
using Axie.Core.HexMap;
using Axie.Utilities;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Axie.Core
{
    /// <summary>
    /// Game Controller to manage game flow
    /// </summary>
    public class GameController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private AxieCatalog axieCatalog;
        [SerializeField] private HexMapController mapController;

        [SerializeField] private int mapRadius;
        [SerializeField] private Transform attackerGroup;
        [SerializeField] private Transform defenderGroup;

        [SerializeField] private TextMeshProUGUI labePause;
        [SerializeField] private TextMeshProUGUI labeSpeed;
        [SerializeField] private TextMeshProUGUI labelFps;
        [SerializeField] private Slider slider;

        [SerializeField] private FPSManager fpsManager;
        [SerializeField] private PowerBar powerBar;

        private float[] preDefineTimeScaleRangeArray = new float[5] { 0, 0.5f, 1, 2, 5 };

        private GameManager gameManager;

        [SerializeField] private Dictionary<Hex, AxieController> axieControllers = new Dictionary<Hex, AxieController>();

        private Dictionary<string, AxiePool> poolDict = new Dictionary<string, AxiePool>();

        private int initialAttackCount = 0;
        private int initialDefCount = 0;

        #endregion

        #region Unity Events

        private void Awake()
        {
            mapController.InitMap(mapRadius);

            foreach (var config in axieCatalog.Config)
            {
                var pool = new AxiePool();
                pool.Init(config.Value.prefab, 20, config.Value.faction == FactionType.Attacker ? attackerGroup
                                                                                                    : defenderGroup);

                poolDict.Add(config.Key, pool);
            }

            gameManager = new GameManager(axieCatalog, mapController.Map);
            gameManager.OnAxieBorn += OnAxieBorn;
            gameManager.OnAxieMove += OnAxieMove;
            gameManager.OnAxieAttack += OnAxieAttack;
            gameManager.OnAxieIdle += OnAxieIdle;
            gameManager.OnMapExpanded += OnMapExpanded;
            gameManager.OnTurnInfoUpdate += OnTurnInfoUpdated;
            gameManager.Initialize();

            slider.value = 3;
        }

        private void Start()
        {
            powerBar.UpdatePower(initialAttackCount, initialDefCount);
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.End();
            }
        }

        private void Update()
        {
            labelFps.text = $"FPS {fpsManager.FPS}";

            if (gameManager != null)
            {
                gameManager.fps = fpsManager.FPS;
                gameManager.Update();
            }
        }

        #endregion

        #region Public Methods

        public void OnPausToggle()
        {
            if (Time.timeScale == 0)
            {
                labePause.text = "Pause";
                Time.timeScale = 1f;
            }
            else
            {
                labePause.text = "UnPause";
                Time.timeScale = 0;
            }
        }

        public void OnTimeScaleChanged(float value)
        {
            Time.timeScale = preDefineTimeScaleRangeArray[(int)value - 1];
            labeSpeed.text = $"Speed {Time.timeScale}";
        }

        #endregion

        #region Private Methods

        private void OnAxieDead(AxieModel axie)
        {
            var deadAxie = axieControllers[axie.hex];
            deadAxie.Die(() =>
            {
                axieControllers.Remove(axie.hex);
                poolDict[axie.configId].Recycle(deadAxie.gameObject);
            });
        }

        private void OnAxieBorn(AxieModel axieModel)
        {
            var axie = poolDict[axieModel.configId].GetItem();
            var controller = axie.GetComponent<AxieController>();
            controller.Setup(axieModel.hex, axieModel.maxHp);
            controller.SetPosition(mapController.Map.HexToPixel(axieModel.hex), true);

            if (axieModel.faction == FactionType.Attacker)
            {
                initialAttackCount += 1;
            }
            else
            {
                initialDefCount += 1;
            }

            axieControllers.Add(axieModel.hex, controller);
        }

        private void OnAxieIdle(AxieModel axie)
        {
            //throw new System.NotImplementedException();
            //Debug.LogError("Idle");
        }

        private void OnAxieAttack(BattleData attacker, BattleData defender)
        {
            var attackAxie = axieControllers[attacker.axie.hex];
            var defendAxie = axieControllers[defender.axie.hex];

            attackAxie.Facing(defendAxie.transform.position);
            attackAxie.Attack(defendAxie.transform, () =>
             {
                 if (attacker.newHp > 0
                    && !attacker.isDie)
                 {
                     attackAxie.Idle();
                 }
                 defendAxie.OnTakeDamage(defender.newHp);
                 if (defender.isDie)
                 {
                     OnAxieDead(defender.axie);
                 }
             });

            if (defender.damageDealt > 0)
            {
                defendAxie.Facing(attackAxie.transform.position);

                defendAxie.Attack(attackAxie.transform, () =>
                {
                    if (defender.newHp > 0
                        && !defender.isDie)
                    {
                        defendAxie.Idle();
                    }
                    attackAxie.OnTakeDamage(attacker.newHp);
                    if (attacker.isDie)
                    {
                        OnAxieDead(attacker.axie);
                    }
                });
            }
        }

        private void OnAxieMove(int turn, AxieModel axie, Hex oldPos, Hex newPos)
        {
            var moveAxie = axieControllers[oldPos];
            axieControllers.Remove(oldPos);
            axieControllers[newPos] = moveAxie;

            var newCellPos = mapController.Map.HexToPixel(newPos);
            moveAxie.Facing(newCellPos);
            moveAxie.Move(newCellPos);
        }

        private void OnMapExpanded(Dictionary<Hex, AxieModel> newAddedList)
        {
            StartCoroutine(SpawnNewRoutine(newAddedList));
        }

        private IEnumerator SpawnNewRoutine(Dictionary<Hex, AxieModel> newAddedList)
        {
            int maxSpawnPerFrame = 40;
            int count = 0;

            foreach (var item in newAddedList)
            {
                mapController.AddNewCell(item.Key);
                if (item.Value != null)
                {
                    var axie = poolDict[item.Value.configId].GetItem();
                    var controller = axie.GetComponent<AxieController>();

                    controller.Setup(item.Key, item.Value.maxHp);
                    controller.SetPosition(mapController.Map.HexToPixel(item.Key), true);

                    axieControllers.Add(item.Key, controller);
                }
                count += 1;
                if (count >= maxSpawnPerFrame)
                {
                    count = 0;
                    yield return null;
                }
            }

            gameManager.waitMapExpand = false;
        }


        private void OnTurnInfoUpdated(int attackCount, int defendCount)
        {
            powerBar.UpdatePower(attackCount, defendCount);
        }

        #endregion
    }
}