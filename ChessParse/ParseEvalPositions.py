import sys

import chess.pgn
import mysql.connector

db = mysql.connector.connect(
    host="localhost",
    user=sys.argv[1],
    password=sys.argv[2],
    database="chess",
)

totalGames = 95300285
# 2.56%
parsedPercent = 256

evalPositions = 0
games = 0
percent = 0
lastPercent = 0

with open("lichess_db_standard_rated_2023-07.pgn") as pgn:
    while True:
        game = chess.pgn.read_game(pgn)
        if game is None:
            break

        games += 1
        percent = (int)(games / totalGames * 10000)
        if percent > lastPercent:
            print(percent / 100, "%, evals:", evalPositions)
        lastPercent = percent

        if percent < parsedPercent:
            continue

        board = game.board()
        position = game

        while True:
            position = position.next()
            if position is None:
                break
            board.push(position.move)

            eval = position.eval()
            if eval is None:
                break

            if not eval.is_mate():
                fen = board.fen()
                eval = eval.relative.score()

                cursor = db.cursor()
                cursor.execute(
                    "SELECT eval FROM evals WHERE fen = %s",
                    (board.fen(),),
                )
                result = cursor.fetchone()

                if result is None:
                    cursor = db.cursor()
                    sql = "INSERT INTO evals (fen, eval) VALUES (%s, %s)"
                    val = (fen, eval)
                    cursor.execute(sql, val)
                    db.commit()
                    evalPositions += 1

                elif result[0] != eval and abs(eval - result[0] > 100):
                    print(
                        "Mismatched eval: ",
                        fen,
                        "existing",
                        result[0],
                        "new",
                        eval,
                    )

                    cursor = db.cursor()
                    sql = "UPDATE evals SET ambiguous = %s WHERE fen = %s"
                    val = (True, fen)
                    cursor.execute(sql, val)
                    db.commit()
