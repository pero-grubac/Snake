using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Snake
{
    public class GameState
    {
        public int Rows { get; }
        public int Columns { get; }
        public GridValue[,] GridValues { get; }
        public Direction Direction { get; private set; }
        public int Score { get; private set; }
        public bool GameOver { get; private set; }

        private readonly LinkedList<Position> snakePositions = new LinkedList<Position>();
        private readonly Random random = new Random();
        private readonly LinkedList<Direction> directionChanges = new LinkedList<Direction>();

        public GameState(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            GridValues = new GridValue[Rows, Columns];
            Direction = Direction.Right;
            AddSnake();
            AddFood();
        }
        private void AddSnake()
        {
            int r = Rows / 2;
            for (int col = 1; col <= 3; col++)
            {
                GridValues[r, col] = GridValue.Snake;
                snakePositions.AddFirst(new Position(r, col));
            }
        }
        private IEnumerable<Position> EmptyPositions()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    if (GridValues[row, col] == GridValue.Emplty)
                    {
                        yield return new Position(row, col);
                    }
                }
            }
        }

        private void AddFood()
        {
            List<Position> empty = new List<Position>(EmptyPositions());
            if (empty.Count == 0) return;

            Position pos = empty[random.Next(empty.Count)];
            GridValues[pos.Row, pos.Column] = GridValue.Food;
        }

        public Position HeadPosition()
        {
            return snakePositions.First.Value;
        }
        public Position TailPosition()
        {
            return snakePositions.Last.Value;
        }
        public IEnumerable<Position> SnakePositions()
        {
            return snakePositions;
        }
        public void AddHead(Position pos)
        {
            snakePositions.AddFirst(pos);
            GridValues[pos.Row, pos.Column] = GridValue.Snake;
        }
        public void RemoveTail()
        {
            Position tail = snakePositions.Last.Value;
            GridValues[tail.Row, tail.Column] = GridValue.Emplty;
            snakePositions.RemoveLast();
        }
        public void ChangeDirection(Direction direction)
        {
            if (CanChangeDirection(direction))
            {
                directionChanges.AddLast(direction);
            }
        }
        private bool CanChangeDirection(Direction newDirection)
        {
            if (directionChanges.Count > 0)
            {
                return false;
            }
            Direction lastDirection = GetLastDirection();
            return lastDirection != newDirection && newDirection != lastDirection.Opposite();
        }
        private Direction GetLastDirection()
        {
            if (directionChanges.Count == 0)
            {
                return Direction;
            }
            return directionChanges.Last.Value;
        }
        private bool OutsideGrid(Position position)
        {
            return position.Row < 0 || position.Row >= Rows || position.Column < 0 || position.Column >= Columns;
        }

        private GridValue WillHit(Position newHeadPosition)
        {
            if (OutsideGrid(newHeadPosition)) return GridValue.Outside;
            if (newHeadPosition == TailPosition())
            {
                return GridValue.Emplty;
            }

            return GridValues[newHeadPosition.Row, newHeadPosition.Column];
        }
        public void Move()
        {
            if(directionChanges.Count > 0)
            {
                Direction = directionChanges.First.Value;
                directionChanges.RemoveFirst();
            }
            Position newHeadPosition = HeadPosition().Translate(Direction);
            GridValue hit = WillHit(newHeadPosition);

            if (hit == GridValue.Outside || hit == GridValue.Snake)
            {
                GameOver = true;
            }
            else if (hit == GridValue.Emplty)
            {
                RemoveTail();
                AddHead(newHeadPosition);
            }
            else if (hit == GridValue.Food)
            {
                AddHead(newHeadPosition);
                Score++;
                AddFood();
            }
        }
    }
}
