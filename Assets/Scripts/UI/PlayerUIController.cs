using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIController : MonoBehaviour
{
    [SerializeField]
    private Slider _sliderHP;

    [SerializeField]
    private Image _imageTarget;

    [SerializeField]
    private TextMeshProUGUI _labelCoordinates;

    [SerializeField]
    private TextMeshProUGUI _labelHP;

    [SerializeField]
    private TextMeshProUGUI _labelCoins;


    private Canvas _canvas;

    public bool IsTarget
    {
        get => _imageTarget.enabled;
        set { _imageTarget.enabled = value; }
    }

    [HideInInspector]
    public PlayerController player;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
    }

    private void OnEnable()
    {
        if (player == null)
        {
            player = GetComponent<PlayerController>();
        }
        player.statUpdateAction += onPlayerDataValueChange;
        onPlayerDataValueChange();
        InvokeRepeating(nameof(updateLocation), 0, 0.1f);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(updateLocation));
        player.statUpdateAction -= onPlayerDataValueChange;
        player = null;
    }

    private void onPlayerDataValueChange()
    {
        _sliderHP.maxValue = player.maxHP;
        _sliderHP.value = player.HP;
        if (_labelHP != null)
        {
            _labelHP.text = string.Format("{0} / {1}", player.HP, player.maxHP);
        }
    }

    private void updateLocation()
    {
        _labelCoordinates.text = string.Format(
            "{0}: {1:0.00}, {2:0.00}",
            player.playerName,
            player.transform.position.x,
            player.transform.position.z);

        if (_labelCoins != null)
        {
            _labelCoins.text = player.Coins.ToString();
        }
    }

    private void Update()
    {
        if (_canvas != null)
        {
            _canvas.transform.LookAt(GameController.Instance.cameraThirdPerson.transform);
        }
    }
}
