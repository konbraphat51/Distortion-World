using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Invoke each UnityEvent when corresponding key pushed
/// </summary>
public class InputManager : SingletonMonoBehaviour<InputManager>
{
    //Simply push
    [SerializeField] private KeyAction[] simpleActions;

    //Actions require several pushes (including single)
    [SerializeField] private KeyActionSeveralPushes[] severalPushesActions;

    [Tooltip("longest interval of several pushes(s)")]
    [SerializeField] private float pushesInterval = 0.3f;

    //get time of interval of key pushes 
    private List<KeyTimer> keyTimers = new List<KeyTimer>();

    //remember whether the key is pushed
    //keyname -> isPushed
    private Dictionary<string, bool> isPushed = new Dictionary<string, bool>();

    //remember whether the key was pushed 1 frame before
    //keyname -> isPushed
    private Dictionary<string, bool> wasPushed = new Dictionary<string, bool>();

    //pushed by Push() (update each frame)
    //contains keyName
    private List<string> functionPushed = new List<string>();

    //remember corresponding key to avoid duplicated search at PushForAction()
    private Dictionary<UnityAction, string> actionToKey = new Dictionary<UnityAction, string>();

    [Tooltip("Button Name -> Key Name")]
    [SerializeField] private Dictionary<string, string> buttonsInterpretations = new Dictionary<string, string>();

    //contains infomations of several push keys
    private class KeyTimer
    {
        public string keyName;
        public int pushedNum = 0;

        //time passed after the latest push
        public float timer = 0f;

        //time passed after the first push
        public float totalTimer = 0f;

        //contructor
        public KeyTimer(string keyName)
        {
            this.keyName = keyName;
        }
    }

    //running SeveralPushesAction
    //Action in this list will be runned.
    //Will be deleted when key up.
    //key: key name
    private List<KeyActionSeveralPushes> runningActions
        = new List<KeyActionSeveralPushes>();

    //keys has severalPushesActions
    private List<string> severalPushesKeys = new List<string>();

    private void Start()
    {
        InitializeSeveralPushesKeys();

        UpdateIsPushedIndex();
    }

    private void Update()
    {
        UpdateKeys();
        RunActions();        
    }

    /// <summary>
    /// Update isPushed by GetKey + GetButton + Push functions
    /// </summary>
    private void UpdateKeys()
    {
        //update wasPushed (1 frame before)
        wasPushed = new Dictionary<string, bool>(isPushed);

        //reset
        foreach (string key in wasPushed.Keys)
        {
            isPushed[key] = false;
        }

        //check key
        foreach(string keyName in wasPushed.Keys)
        {
            if (Input.GetKey(keyName))
            {
                isPushed[keyName] = true;
            }
        }

        //check buttons
        foreach(string buttonName in buttonsInterpretations.Keys)
        {
            if (Input.GetButton(buttonName))
            {
                isPushed[buttonsInterpretations[buttonName]] = true;
            }
        }

        //check function Push
        foreach(string keyName in functionPushed)
        {
            isPushed[keyName] = true;
        }

        //reset
        functionPushed = new List<string>();
    }

    /// <summary>
    /// Push by functions
    /// ex) for EventTrigger
    /// </summary>
    public static void Push(string keyName)
    {
        Instance.functionPushed.Add(keyName);
    }

    /// <summary>
    /// Run actions if pushed
    /// </summary>
    private void RunActions()
    {
        RunSimpleActions();
        CheckSeveralPushesIncreases();

        CheckTimers();

        RunSeveralPushesActions();
    }

    /// <summary>
    /// check if pushed and invoke of simpleActions
    /// </summary>
    private void RunSimpleActions()
    {
        foreach(KeyAction keyAction in simpleActions)
        {
            //correspond to input type
            switch (keyAction.inputType)
            {
                case KeyAction.KeyType.GetKey:
                    //Invoke during pushed
                    if(GetKey(keyAction.keyName)){
                        keyAction.Invoke();
                    }
                    break;

                case KeyAction.KeyType.GetKeyDown:
                    //Invoke right on the time of pushed
                    if(GetKeyDown(keyAction.keyName)){
                        keyAction.Invoke();
                    }
                    break;

                case KeyAction.KeyType.GetKeyUp:
                    //Invoke right on the time key up
                    if (GetKeyUp(keyAction.keyName))
                    {
                        keyAction.Invoke();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Get GetKeyDown of severalPushActions for increase the push counter
    /// </summary>
    private void CheckSeveralPushesIncreases()
    {
        foreach (string keyName in severalPushesKeys)
        {
            if (GetKeyDown(keyName))
            {
                //counter increases
                KeyTimer keyTimer = GetTimer(keyName);
                keyTimer.pushedNum++;

                //reset timer
                keyTimer.timer = 0f;

                ////run immedietly
                KeyActionSeveralPushes correspondingAction = GetSeveralPushesAction(keyName, keyTimer.pushedNum);
                //if exists and dont wait
                if (correspondingAction != null && correspondingAction.dontWait)
                {
                    runningActions.Add(correspondingAction);
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private KeyActionSeveralPushes GetSeveralPushesAction(string keyName, int pushesNum)
    {
        foreach(KeyActionSeveralPushes keyAction in severalPushesActions)
        {
            if ((keyAction.keyName == keyName) && (keyAction.pushesNum == pushesNum))
            {
                //found
                return keyAction;
            }
        }

        //couldn't find
        return null;
    }

    /// <summary>
    /// Return reference of corresponding KeyTimer from keyTimers.
    /// Create one if none found.
    /// </summary>
    private KeyTimer GetTimer(string keyName)
    {
        foreach(KeyTimer keyTimer in keyTimers)
        {
            if(keyTimer.keyName == keyName)
            {
                //found
                return keyTimer;
            }
        }

        //not found
        
        //add new timer
        KeyTimer newTimer = new KeyTimer(keyName);
        keyTimers.Add(newTimer);
        return newTimer;
    }

    /// <summary>
    /// Check keyTimers and start running if there is exceeding limit
    /// </summary>
    private void CheckTimers()
    {
        List<KeyTimer> _keyTimers
            = new List<KeyTimer>(keyTimers);
        foreach(KeyTimer keyTimer in _keyTimers)
        {
            //advance timer
            keyTimer.timer += Time.deltaTime;

            //check if exceeding
            if (keyTimer.timer >= pushesInterval)
            {
                //exceeded

                //get rid of same key action if exists
                List<KeyActionSeveralPushes> _runningActions
                    = new List<KeyActionSeveralPushes>(runningActions);
                foreach (KeyActionSeveralPushes action in _runningActions)
                {
                    if (action.keyName == keyTimer.keyName)
                    {
                        //found
                        runningActions.Remove(action);
                    }
                }

                ////start running
                //get corresponding action
                foreach (KeyActionSeveralPushes action in severalPushesActions)
                {
                    if ((action.keyName == keyTimer.keyName)
                        && (action.pushesNum == keyTimer.pushedNum))
                    {
                        //corresponded

                        //start running
                        runningActions.Add(action);
                    }
                }

                //get rid of timer
                keyTimers.Remove(keyTimer);
            }
        }
    }

    /// <summary>
    /// run the severalPushes
    /// </summary>
    private void RunSeveralPushesActions()
    {
        List<KeyActionSeveralPushes> _runningActions
            = new List<KeyActionSeveralPushes>(runningActions);
        foreach(KeyActionSeveralPushes action in _runningActions)
        {
            switch (action.inputType)
            {
                case KeyAction.KeyType.GetKey:
                    //invoke while pushed
                    if (GetKey(action.keyName))
                    {
                        action.Invoke();
                    }
                    else
                    {
                        //not push => delete
                        runningActions.Remove(action);
                    }
                    break;

                case KeyAction.KeyType.GetKeyDown:
                    //run immedietly
                    action.Invoke();
                    runningActions.Remove(action);
                    break;

                case KeyAction.KeyType.GetKeyUp:
                    //run when up
                    //not using GetKeyUp because of possibility of
                    //key up during waiting for keyTimer
                    if (GetKeyUp(action.keyName))
                    {
                        action.Invoke();
                        runningActions.Remove(action);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Call this when actions changed
    /// </summary>
    private void InitializeSeveralPushesKeys()
    {
        List<string> newList = new List<string>();

        foreach(KeyActionSeveralPushes keyAction in severalPushesActions)
        {
            newList.Add(keyAction.keyName);
        }

        //get rid of duplicates
        severalPushesKeys = newList.Distinct().ToList();
    }

    /// <summary>
    /// Check if there is index missing in isPushed, and add the missing index
    /// </summary>
    private void UpdateIsPushedIndex()
    {
        foreach(KeyAction keyAction in simpleActions)
        {
            if (!isPushed.ContainsKey(keyAction.keyName))
            {
                //doesn't contain
                //add the index
                isPushed[keyAction.keyName] = false;
            }

            if (!wasPushed.ContainsKey(keyAction.keyName))
            {
                //doesn't contain
                //add the index
                wasPushed[keyAction.keyName] = false;
            }
        }

        foreach(KeyActionSeveralPushes keyAction in severalPushesActions)
        {
            if (!isPushed.ContainsKey(keyAction.keyName))
            {
                //doesn't contain
                //add the index
                isPushed[keyAction.keyName] = false;
            }

            if (!wasPushed.ContainsKey(keyAction.keyName))
            {
                //doesn't contain
                //add the index
                wasPushed[keyAction.keyName] = false;
            }
        }
    }

    /// <summary>
    /// return true while pushed
    /// </summary>
    private bool GetKey(string keyName)
    {
        if (isPushed[keyName])
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// return true only the first frame pushed
    /// </summary>
    private bool GetKeyDown(string keyName)
    {
        if (!wasPushed[keyName] && isPushed[keyName])
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// return true only the first frame stop pushed
    /// </summary>
    private bool GetKeyUp(string keyName)
    {
        if (wasPushed[keyName] && !isPushed[keyName])
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Actions and its key
    /// </summary>
    [System.Serializable] public class KeyAction
    {
        public enum KeyType
        {
            GetKey,
            GetKeyDown,
            GetKeyUp
        }

        [Tooltip("Methods called when key pushed")]
        [SerializeField] private UnityEvent _actions;

        [Tooltip("Key name. Google 'Unity Input key name'")]
        [SerializeField] private string _keyName;

        [Tooltip("0:GetKey, 1:GetKeyDown, 2:GetKeyUp")]
        [SerializeField] private KeyType _inputType = 0;
        
        //getters. These are necessary for SerializeField
        public string keyName
        {
            get
            {
                return _keyName;
            }
        }

        public KeyType inputType
        {
            get
            {
                return _inputType;
            }
        }

        public UnityEvent actions
        {
            get
            {
                return _actions;
            }
        }

        /// <summary>
        /// Simply invoke the registered UnityActions
        /// </summary>
        public void Invoke()
        {
            actions.Invoke();
        }
    }

    /// <summary>
    /// Actions and its key. For Several push.
    /// Single push must registered by this when there is also double pushes for the same key.
    /// </summary>
    [System.Serializable] public class KeyActionSeveralPushes : KeyAction
    {
        [Tooltip("How many pushes required")]
        [SerializeField] private int _pushesNum = 1;

        [Tooltip("if true, the action invoked before next push. The invoke will cancel when next push appear")]
        [SerializeField] private bool _dontWait = false;

        //getters
        public int pushesNum
        {
            get
            {
                return _pushesNum;
            }
        }

        public bool dontWait
        {
            get
            {
                return _dontWait;
            }
        }
    }
}
