namespace Planner.Optimization.Contracts.Helpers;

public static class MatrixConverter {
    public static double[][] ToJagged(double[,] matrix) {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        var result = new double[rows][];
        for (int i = 0; i < rows; i++) {
            result[i] = new double[cols];
            for (int j = 0; j < cols; j++)
                result[i][j] = matrix[i, j];
        }
        return result;
    }

    public static double[,] ToRectangular(double[][] matrix) {
        int rows = matrix.Length;
        int cols = matrix[0].Length;
        var result = new double[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                result[i, j] = matrix[i][j];
        return result;
    }
}
