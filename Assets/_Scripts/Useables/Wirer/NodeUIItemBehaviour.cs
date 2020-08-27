using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NodeUIItemBehaviour : MonoBehaviour {
	public Text direction;
	public Text valueType;
	public new Text name;
	public Text description;
	public Image image;

	private Node _Node;

	private bool _Selected = false;

	public Node Node {
		get => _Node;
		set {
			_Node = value;
			direction.text = value.GetDirection();
			valueType.text = value.GetValueType();
			name.text = value.name;
			description.text = value.description;
			gameObject.name = $"{value.name} UI";
		}
	}

	public bool Selected {
		get => _Selected;
		set {
			if (_Selected != value) {
				_Selected = value;
				if (image)
					if (value) {
						image.color = new Color(191f / 255f, 250f / 255f, 255f / 255f, 0.5f);
					} else {
						image.color = new Color(1, 1, 1, 0.5f);
					}
			}
		}
	}
}