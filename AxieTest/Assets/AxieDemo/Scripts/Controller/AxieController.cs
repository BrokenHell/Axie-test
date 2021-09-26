using System.Collections.Generic;
using Axie.Core.HexMap;
using Axie.Core.Simulation;
using Spine.Unity;
using UnityEngine;

namespace Axie.Core
{
    public class AxieController : MonoBehaviour
    {
        [System.Serializable]
        public enum State
        {
            Idle,
            Move,
            Attack,
            Dead
        }

        #region Fields

        [SerializeField] private SkeletonAnimation spineAnimation;
        [SerializeField] private Transform cachedTransform;
        [SerializeField] private string[] attackAnimation;
        [SerializeField] private string moveAnimation;
        [SerializeField] private string idleAnimation;
        [SerializeField] private string deadAnimation;
        [SerializeField] private State currentState = State.Idle;
        [SerializeField] private HPBar hpBar;
        [SerializeField] private float moveSpeed;
        [SerializeField] private MeshRenderer renderer;

        private bool canPlayAnimation;
        private Hex hexPos;

        private System.Action attackAnimationCallback;
        private System.Action deadAnimationCallback;

        public Hex HexPosition => hexPos;

        private int maxHp;

        private bool hasListenEvent;

        [SerializeField]
        private Vector3 moveTarget;
        private Vector3 moveDir;

        private Transform target;

        [SerializeField]
        private int currentHP;

        //private bool isInvisible;

        #endregion

        #region Unity Events

        private void OnEnable()
        {
            if (spineAnimation.AnimationState != null && !hasListenEvent)
            {
                hasListenEvent = true;
                spineAnimation.AnimationState.Complete += AnimationComplete;
            }
        }

        private void Start()
        {
            spineAnimation.timeScale = 2f;
            if (spineAnimation.AnimationState != null && !hasListenEvent)
            {
                hasListenEvent = true;
                spineAnimation.AnimationState.Complete += AnimationComplete;
            }
        }

        private void OnDisable()
        {
            spineAnimation.AnimationState.Complete -= AnimationComplete;
            hasListenEvent = false;
        }

        private void OnBecameVisible()
        {
            //isInvisible = false;
            canPlayAnimation = true;
            spineAnimation.enabled = true;
            spineAnimation.gameObject.SetActive(true);
            switch (currentState)
            {
                case State.Idle: Idle(); break;
                case State.Move: Move(moveTarget); break;
                case State.Attack: Attack(target); break;
                case State.Dead: Die(); break;
            }
        }

        private void OnBecameInvisible()
        {
            canPlayAnimation = false;
            spineAnimation.enabled = false;
            spineAnimation.gameObject.SetActive(false);
            //isInvisible = true;
        }

        #endregion

        #region Public Methods

        private void Update()
        {
            //if (!isInvisible)
            //{
            //    if (!IsVisible())
            //    {
            //        OnBecameInvisible();
            //    }
            //}

            //if (isInvisible)
            //{
            //    if (IsVisible())
            //    {
            //        OnBecameVisible();
            //    }
            //}

            if (currentState == State.Move)
            {
                if (canPlayAnimation)
                {


                    float step = moveSpeed * Time.deltaTime; // calculate distance to move
                    transform.position = Vector3.MoveTowards(transform.position, moveTarget, step);

                    //var pos = this.transform.position;
                    //pos += moveDir * Time.deltaTime * moveSpeed;
                    if (Vector3.Distance(transform.position, moveTarget) <= 0.01f)
                    {
                        transform.position = moveTarget;
                        this.Idle();
                    }
                    //this.transform.position = pos;
                }
                else
                {
                    this.transform.position = moveTarget;
                    Idle();
                }
            }
        }

        public void Setup(Hex hex, int maxHp)
        {
            this.currentHP = maxHp;
            this.maxHp = maxHp;
            this.hexPos = hex;
            this.hpBar.SetValue(maxHp, maxHp);
            canPlayAnimation = true;
            spineAnimation.enabled = true;
            currentState = State.Idle;
            this.Idle();
        }

        public void SetPosition(Vector3 position, bool instant = false)
        {
            if (instant)
            {
                this.transform.position = new Vector3(position.x, position.y - 0.12f, position.z);
            }
        }

        public void Attack(Transform target, System.Action callback = null)
        {
            if (currentState == State.Dead)
                return;
            currentState = State.Attack;
            if (canPlayAnimation)
            {
                attackAnimationCallback = callback;
                this.target = target;
                spineAnimation.AnimationState.SetAnimation(0,
                                        attackAnimation[Random.Range(0, attackAnimation.Length)],
                                        false);
            }
            else
            {
                callback?.Invoke();
            }
        }

        public void OnTakeDamage(int newHp)
        {
            if (this.currentState == State.Dead)
                return;
            if (newHp > this.currentHP)
                return;
            this.currentHP = newHp;
            hpBar.SetValueAnimated(newHp, this.maxHp);
        }

        public void Move(Vector3 position)
        {
            if (currentState == State.Dead)
                return;
            moveTarget = position;
            currentState = State.Move;
            moveDir = (position - this.transform.position).normalized;
            if (!canPlayAnimation)
                return;
            spineAnimation.AnimationState.SetAnimation(0, moveAnimation, true);
        }

        public void Idle()
        {
            if (currentState == State.Dead)
                return;

            currentState = State.Idle;
            if (!canPlayAnimation)
                return;
            spineAnimation.AnimationState.SetAnimation(0, idleAnimation, true);
        }

        public void Die(System.Action callback = null)
        {
            currentState = State.Dead;
            if (canPlayAnimation)
            {
                deadAnimationCallback = callback;
                spineAnimation.AnimationState.SetAnimation(0, deadAnimation, false);
            }
            else
            {
                callback?.Invoke();
            }
        }

        public void Facing(Vector3 enemyPosition)
        {
            if (enemyPosition.x > cachedTransform.position.x)
            {
                spineAnimation.transform.localEulerAngles = new Vector3(0, 180, 0);
            }
            else
            {
                spineAnimation.transform.localEulerAngles = Vector3.zero;
            }
        }

        #endregion

        #region Private Methods

        private void AnimationComplete(Spine.TrackEntry trackEntry)
        {
            if (currentState == State.Attack)
            {
                this.target = null;
                attackAnimationCallback?.Invoke();
                attackAnimationCallback = null;
            }
            else if (currentState == State.Dead)
            {
                attackAnimationCallback?.Invoke();
                deadAnimationCallback?.Invoke();
                deadAnimationCallback = null;
            }
        }

        //private void OnDrawGizmos()
        //{
        //    if (this.target != null)
        //    {
        //        Gizmos.color = Color.red;
        //        Gizmos.DrawLine(transform.position, target.position);
        //    }
        //}

        //bool IsVisible()
        //{
        //    var plans = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        //    return GeometryUtility.TestPlanesAABB(plans, renderer.bounds);
        //}

        #endregion
    }
}