using Godot;
using System;

public partial class CardManager : Node2D {
    Card draggedCard;
    private bool lastTouchSelectedCard = false;

    public override void _Process(double delta) {
        if (draggedCard != null) {
            var touchPos = GetGlobalMousePosition();
            draggedCard.Position = touchPos;
        }
    }

    public override void _Input(InputEvent @event) {
        if (@event is InputEventScreenTouch touch) {
            if (touch.Pressed) {
                var card = GetCardOnTouchPosition(touch.Position);
                if (card != null) {
					if (card.State == CardState.Idle) {
						foreach (var child in GetChildren()) {
							if (child is Card c && c != card) {
								c.State = CardState.Idle;
							}
						}
						card.State = CardState.Selected;
						lastTouchSelectedCard = true;
					}
					else if (card.State == CardState.Selected) {
						draggedCard = card;
                    	lastTouchSelectedCard = false;
					}
                }
                else {
                    foreach (var child in GetChildren()) {
                        if (child is Card c) c.State = CardState.Idle;
                    }
                    lastTouchSelectedCard = false;
                }
            }
            else {
                if (draggedCard != null) {
                    draggedCard.State = CardState.Idle;
                    draggedCard = null;
                }
                lastTouchSelectedCard = false;
            }
        }
        else if (@event is InputEventScreenDrag drag) {
            // If we're already dragging, continue moving the card
            if (draggedCard != null) {
                draggedCard.Position = drag.Position;
            }
            else {
                // Only start dragging if the card under the drag is already Selected and the drag is NOT
                // part of the same touch that selected it (enforce two separate motions).
                var card = GetCardOnTouchPosition(drag.Position);
                if (card != null && card.State == CardState.Selected && !lastTouchSelectedCard) {
                    draggedCard = card;
                    draggedCard.State = CardState.Dragged;
                    draggedCard.Position = drag.Position;
                }
            }
        }
    }
    
    private Card GetCardOnTouchPosition(Vector2 position) {
        var spaceState = GetWorld2D().DirectSpaceState;
        var parameters = new PhysicsPointQueryParameters2D();
        parameters.Position = position;
        parameters.CollideWithAreas = true;
        parameters.CollisionMask = 1;
        var result = spaceState.IntersectPoint(parameters);
        if (result == null || result.Count == 0) {
            return null;
        }
        Area2D collider = (Area2D) result[0]["collider"];
        return (Card) collider.GetParent();
    }
    
}