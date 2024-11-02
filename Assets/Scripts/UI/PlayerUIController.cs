using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIController : MonoBehaviour
{
    [SerializeField]
    private Canvas _canvas;

    [SerializeField]
    private Slider _sliderHP;

    [SerializeField]
    private Image _imageTarget;

    [SerializeField]
    private TextMeshProUGUI _labelCoordinates;

    public bool IsTarget
    {
        get => _imageTarget.enabled;
        set { _imageTarget.enabled = value; }
    }

    private PlayerController _player;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        _player.statUpdateAction += onPlayerDataValueChange;
        onPlayerDataValueChange();
        InvokeRepeating(nameof(updateLocation), 0, 0.1f);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(updateLocation));
        _player.statUpdateAction -= onPlayerDataValueChange;
    }

    private void onPlayerDataValueChange()
    {
        _sliderHP.maxValue = _player.maxHP;
        _sliderHP.value = _player.HP;
    }

    private void updateLocation()
    {
        _labelCoordinates.text = string.Format(
            "{0}: {1:0.00}, {2:0.00}",
            _player.playerName,
            transform.position.x,
            transform.position.z);
    }

    private void Update()
    {
        _canvas.transform.LookAt(GameController.Instance.cameraThirdPerson.transform);
    }
}
